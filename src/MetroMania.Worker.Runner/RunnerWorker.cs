using System.Diagnostics;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Azure.Messaging.ServiceBus;
using MediatR;
using MetroMania.Application.DTOs;
using MetroMania.Application.Levels.Queries;
using MetroMania.Application.Messages;
using MetroMania.Application.Submissions.Commands;
using MetroMania.Application.Submissions.Queries;
using MetroMania.Domain.Entities;
using MetroMania.Domain.Enums;
using MetroMania.Domain.Extensions;
using MetroMania.Engine.Model;
using MetroMania.Scripting;

namespace MetroMania.Worker.Runner;

public class RunnerWorker(
    IServiceScopeFactory scopeFactory,
    IConfiguration configuration,
    IHttpClientFactory httpClientFactory,
    ILogger<RunnerWorker> logger) : BackgroundService
{
    private static readonly JsonSerializerOptions NotifyJsonOptions = new()
    {
        Converters = { new JsonStringEnumConverter() },
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var connectionString = configuration.GetValue<string>("SERVICE_BUS_CONNECTION_STRING")
            ?? throw new InvalidOperationException("Set the SERVICE_BUS_CONNECTION_STRING environment variable.");
        var queueName = configuration.GetValue<string>("SERVICE_BUS_RUN_QUEUE")
            ?? throw new InvalidOperationException("Set the SERVICE_BUS_RUN_QUEUE environment variable.");

        await using var client = new ServiceBusClient(connectionString);
        await using var processor = client.CreateProcessor(queueName, new ServiceBusProcessorOptions
        {
            AutoCompleteMessages = false,
            MaxConcurrentCalls = 6
        });

        processor.ProcessMessageAsync += async args =>
        {
            var message = args.Message.Body.ToObjectFromJson<ProcessSubmissionMessage>()
                ?? throw new InvalidOperationException("Failed to deserialize submission message.");
            logger.LogInformation("Received run message for submission {SubmissionId}", message.SubmissionId);

            try
            {
                await ProcessSubmissionAsync(message.SubmissionId, stoppingToken);
                await args.CompleteMessageAsync(args.Message, stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error processing run for submission {SubmissionId}", message.SubmissionId);
                await args.AbandonMessageAsync(args.Message, cancellationToken: stoppingToken);
            }
        };

        processor.ProcessErrorAsync += args =>
        {
            logger.LogError(args.Exception, "Service Bus processing error. Source: {Source}", args.ErrorSource);
            return Task.CompletedTask;
        };

        await processor.StartProcessingAsync(stoppingToken);
        logger.LogInformation("Runner worker started, listening on queue '{Queue}'", queueName);

        await Task.Delay(Timeout.Infinite, stoppingToken);

        await processor.StopProcessingAsync(stoppingToken);
    }

    private async Task ProcessSubmissionAsync(Guid submissionId, CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();

        var submission = await sender.Send(new GetSubmissionByIdQuery(submissionId), ct)
            ?? throw new InvalidOperationException($"Submission {submissionId} not found.");

        var levels = await sender.Send(new GetAllLevelsQuery(), ct);
        if (levels.Count == 0)
        {
            logger.LogWarning("No levels found, skipping submission {SubmissionId}", submissionId);
            return;
        }

        // Mark as running
        await sender.Send(new UpdateSubmissionStatusCommand(submissionId,
            RunStatus: RunStatus.Running), ct);
        await NotifyStatusChangeAsync(submissionId, submission.UserId, RunStatus.Running, null);

        logger.LogInformation("Running submission {SubmissionId} against {LevelCount} levels",
            submissionId, levels.Count);

        try
        {
            var base64Code = submission.Code.Base64Encode();

            // Run scripts in parallel across all levels
            var runTasks = levels.Select(level =>
            {
                logger.LogInformation("Running script for level {LevelId} ({LevelTitle})", level.Id, level.Title);
                return RunScriptForLevelAsync(base64Code, level);
            }).ToList();

            var sw = Stopwatch.StartNew();
            var results = await Task.WhenAll(runTasks);
            sw.Stop();
            logger.LogInformation("Ran scripts for {LevelCount} levels in {ElapsedMs}ms",
                levels.Count, sw.ElapsedMilliseconds);

            // Save scores
            var scores = levels.Zip(results, (level, result) =>
                new SaveSubmissionScoresCommand.LevelScore(level.Id, result.Success ? result.Score : 0))
                .ToList();

            await sender.Send(new SaveSubmissionScoresCommand(submissionId, scores), ct);

            // Check for failures
            var failures = levels
                .Zip(results, (level, result) => (level, result))
                .Where(x => !x.result.Success)
                .ToList();

            if (failures.Count > 0)
            {
                var message = string.Join("\n", failures.Select(f =>
                    $"[{f.level.Title}] Run: {f.result.Error}"));

                await sender.Send(new UpdateSubmissionStatusCommand(submissionId,
                    RunStatus: RunStatus.Failed, Message: message), ct);
                await NotifyStatusChangeAsync(submissionId, submission.UserId, RunStatus.Failed, null);
                return;
            }

            for (var i = 0; i < levels.Count; i++)
                logger.LogInformation("Level {LevelTitle}: Score={Score}, Days={Days}, Time={Time}ms",
                    levels[i].Title, results[i].Score, results[i].DaysSurvived, results[i].TimeTakenMs);

            // Mark run as completed
            await sender.Send(new UpdateSubmissionStatusCommand(submissionId,
                RunStatus: RunStatus.Ran), ct);
            await NotifyStatusChangeAsync(submissionId, submission.UserId, RunStatus.Ran, null);

            logger.LogInformation("Submission {SubmissionId} run completed", submissionId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Submission {SubmissionId} run failed", submissionId);
            var message = ex.InnerException?.Message ?? ex.Message;
            await sender.Send(new UpdateSubmissionStatusCommand(submissionId,
                RunStatus: RunStatus.Failed, Message: message), ct);
            await NotifyStatusChangeAsync(submissionId, submission.UserId, RunStatus.Failed, null);
        }
    }

    private static async Task<ScriptRunResult> RunScriptForLevelAsync(string base64Code, LevelDto levelDto)
    {
        try
        {
            var level = ToLevelEntity(levelDto);
            var wrappedScript = WrapInRunScript(base64Code);
            var scriptCompiler = new ScriptCompiler<GameResult>();

            var globals = new ScriptGlobals(level);
            var script = await scriptCompiler.CompileForExecution(wrappedScript);
            var result = await script.Invoke(globals);

            if (result is null)
                return new ScriptRunResult(false, "Script returned null.", 0, 0, 0, 0);

            return new ScriptRunResult(
                true, null,
                result.TotalScore,
                result.ProcessingTime.TotalMilliseconds,
                result.DaysSurvived,
                result.TotalPassengersSpawned);
        }
        catch (Exception ex)
        {
            return new ScriptRunResult(false, ex.InnerException?.Message ?? ex.Message, 0, 0, 0, 0);
        }
    }

    private static string WrapInRunScript(string base64PlayerCode)
    {
        var playerCode = base64PlayerCode.Base64Decode();

        var outerScript = """
            var engine = new MetroManiaEngine();
            var runner = new MyMetroManiaRunner();
            var result = engine.Run(runner, Level, maxHours: Level.LevelData.MaxDays * 24, collectSnapshots: false);
            return result;

            <<PLACEHOLDER>>
            """;

        return outerScript.Replace("<<PLACEHOLDER>>", playerCode).Base64Encode();
    }

    private static Level ToLevelEntity(LevelDto dto) => new()
    {
        Id = dto.Id,
        Title = dto.Title,
        Description = dto.Description,
        GridWidth = dto.GridWidth,
        GridHeight = dto.GridHeight,
        SortOrder = dto.SortOrder,
        LevelData = new LevelData
        {
            BackgroundColor = dto.BackgroundColor,
            WaterColor = dto.WaterColor,
            Seed = dto.Seed,
            VehicleCapacity = dto.VehicleCapacity,
            MaxDays = dto.MaxDays,
            LocalizedContent = dto.LocalizedContent,
            Stations = dto.Stations,
            WaterTiles = dto.WaterTiles,
            WeeklyGiftOverrides = dto.WeeklyGiftOverrides,
            InitialResources = dto.InitialResources
        }
    };

    private async Task NotifyStatusChangeAsync(
        Guid submissionId, Guid userId, RunStatus? runStatus, RenderStatus? renderStatus)
    {
        try
        {
            var client = httpClientFactory.CreateClient("ApiNotify");
            await client.PostAsJsonAsync("/api/submissions/notify",
                new { submissionId, userId, runStatus, renderStatus }, NotifyJsonOptions);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to send notification for submission {SubmissionId}", submissionId);
        }
    }
}

public record ScriptRunResult(
    bool Success, string? Error, int Score,
    double TimeTakenMs, int DaysSurvived, int TotalPassengersSpawned);
