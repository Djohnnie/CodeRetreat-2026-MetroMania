using System.Diagnostics;
using System.Net.Http.Json;
using System.Text.Json;
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
using MetroMania.Infrastructure.Orleans.Services;

namespace MetroMania.Worker;

public class ServiceBusWorker(
    IServiceScopeFactory scopeFactory,
    IConfiguration configuration,
    IHttpClientFactory httpClientFactory,
    ILogger<ServiceBusWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var connectionString = configuration.GetValue<string>("SERVICE_BUS_CONNECTION_STRING")
            ?? throw new InvalidOperationException("Set the SERVICE_BUS_CONNECTION_STRING environment variable.");
        var queueName = configuration.GetValue<string>("SERVICE_BUS_QUEUE")
            ?? throw new InvalidOperationException("Set the SERVICE_BUS_QUEUE environment variable.");

        await using var client = new ServiceBusClient(connectionString);
        await using var processor = client.CreateProcessor(queueName, new ServiceBusProcessorOptions
        {
            AutoCompleteMessages = false,
            MaxConcurrentCalls = 1
        });

        processor.ProcessMessageAsync += async args =>
        {
            var message = args.Message.Body.ToObjectFromJson<ProcessSubmissionMessage>()
                ?? throw new InvalidOperationException("Failed to deserialize submission message.");
            logger.LogInformation("Received submission {SubmissionId}", message.SubmissionId);

            try
            {
                await ProcessSubmissionAsync(message.SubmissionId, stoppingToken);
                await args.CompleteMessageAsync(args.Message, stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error processing submission {SubmissionId}", message.SubmissionId);
                await args.AbandonMessageAsync(args.Message, cancellationToken: stoppingToken);
            }
        };

        processor.ProcessErrorAsync += args =>
        {
            logger.LogError(args.Exception, "Service Bus processing error. Source: {Source}", args.ErrorSource);
            return Task.CompletedTask;
        };

        await processor.StartProcessingAsync(stoppingToken);
        logger.LogInformation("Service Bus worker started, listening on queue '{Queue}'", queueName);

        await Task.Delay(Timeout.Infinite, stoppingToken);

        await processor.StopProcessingAsync(stoppingToken);
    }

    private async Task ProcessSubmissionAsync(Guid submissionId, CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();
        var gameRunnerService = scope.ServiceProvider.GetRequiredService<IGameRunnerService>();
        var gameRendererService = scope.ServiceProvider.GetRequiredService<IGameRendererService>();

        var submission = await sender.Send(new GetSubmissionByIdQuery(submissionId), ct)
            ?? throw new InvalidOperationException($"Submission {submissionId} not found.");

        var levels = await sender.Send(new GetAllLevelsQuery(), ct);
        if (levels.Count == 0)
        {
            logger.LogWarning("No levels found, skipping submission {SubmissionId}", submissionId);
            return;
        }

        // Mark as running
        await sender.Send(new UpdateSubmissionStatusCommand(submissionId, SubmissionStatus.Running), ct);
        await NotifyStatusChangeAsync(submissionId, submission.UserId, SubmissionStatus.Running);
        logger.LogInformation("Processing submission {SubmissionId} against {LevelCount} levels", submissionId, levels.Count);

        try
        {
            var base64Code = submission.Code.Base64Encode();

            // Phase 1: Run scripts
            var runTasks = levels.Select(level =>
            {
                logger.LogInformation("Running script for level {LevelId} ({LevelTitle})", level.Id, level.Title);
                var levelDataJson = JsonSerializer.Serialize(ToLevelEntity(level));
                return gameRunnerService.RunScriptAsync(level.Id, base64Code, levelDataJson);
            }).ToList();

            var runSw = Stopwatch.StartNew();
            var results = await Task.WhenAll(runTasks);
            runSw.Stop();
            logger.LogInformation("Ran scripts for {LevelCount} levels in {ElapsedMs}ms", levels.Count, runSw.ElapsedMilliseconds);

            // Phase 2: Render scripts
            await sender.Send(new UpdateSubmissionStatusCommand(submissionId, SubmissionStatus.Rendering), ct);
            await NotifyStatusChangeAsync(submissionId, submission.UserId, SubmissionStatus.Rendering);

            var renderTasks = levels.Select(level =>
            {
                logger.LogInformation("Rendering script for level {LevelId} ({LevelTitle})", level.Id, level.Title);
                var levelDataJson = JsonSerializer.Serialize(ToLevelEntity(level));
                return gameRendererService.RenderScriptAsync(level.Id, base64Code, levelDataJson);
            }).ToList();

            var renderSw = Stopwatch.StartNew();
            var renderResults = await Task.WhenAll(renderTasks);
            renderSw.Stop();
            logger.LogInformation("Rendered scripts for {LevelCount} levels in {ElapsedMs}ms", levels.Count, renderSw.ElapsedMilliseconds);

            // Phase 3: Save scores and renders
            await sender.Send(new UpdateSubmissionStatusCommand(submissionId, SubmissionStatus.Saving), ct);
            await NotifyStatusChangeAsync(submissionId, submission.UserId, SubmissionStatus.Saving);

            // Build scores from results (score = 0 for failed levels)
            var scores = levels.Zip(results, (level, result) =>
                new SaveSubmissionScoresCommand.LevelScore(level.Id, result.Success ? result.Score : 0))
                .ToList();

            var saveScoresSw = Stopwatch.StartNew();
            await sender.Send(new SaveSubmissionScoresCommand(submissionId, scores), ct);
            saveScoresSw.Stop();
            logger.LogInformation("Saved scores for {LevelCount} levels in {ElapsedMs}ms", levels.Count, saveScoresSw.ElapsedMilliseconds);

            // Save all renders from successful levels
            var allRenders = levels.Zip(renderResults, (level, renderResult) =>
                renderResult.Success
                    ? renderResult.Renders.Select(r => new SaveSubmissionRendersCommand.LevelRender(level.Id, r.Hour, r.SvgContent))
                    : Enumerable.Empty<SaveSubmissionRendersCommand.LevelRender>())
                .SelectMany(r => r)
                .ToList();

            if (allRenders.Count > 0)
            {
                var saveRendersSw = Stopwatch.StartNew();
                await sender.Send(new SaveSubmissionRendersCommand(submissionId, allRenders), ct);
                saveRendersSw.Stop();
                logger.LogInformation("Saved {RenderCount} renders for {LevelCount} levels in {ElapsedMs}ms", allRenders.Count, levels.Count, saveRendersSw.ElapsedMilliseconds);
            }

            // Check if any level run failed
            var runnerFailures = levels
                .Zip(results, (level, result) => (level, result))
                .Where(x => !x.result.Success)
                .ToList();

            var renderFailures = levels
                .Zip(renderResults, (level, result) => (level, result))
                .Where(x => !x.result.Success)
                .ToList();

            if (runnerFailures.Count > 0 || renderFailures.Count > 0)
            {
                var messageLines = new List<string>();

                foreach (var (level, result) in runnerFailures)
                {
                    logger.LogError("Level run failed [{Level}]: {Error}", level.Title, result.Error);
                    messageLines.Add($"[{level.Title}] Run: {result.Error}");
                }

                foreach (var (level, result) in renderFailures)
                {
                    logger.LogError("Level render failed [{Level}]: {Error}", level.Title, result.Error);
                    messageLines.Add($"[{level.Title}] Render: {result.Error}");
                }

                var message = string.Join("\n", messageLines);
                await sender.Send(new UpdateSubmissionStatusCommand(submissionId, SubmissionStatus.Failed, message), ct);
                await NotifyStatusChangeAsync(submissionId, submission.UserId, SubmissionStatus.Failed);
                logger.LogInformation("Submission {SubmissionId} failed", submissionId);
                return;
            }

            for (var i = 0; i < levels.Count; i++)
                logger.LogInformation("Level {LevelTitle}: Score={Score}, Days={Days}, Time={Time}ms, Renders={Renders}",
                    levels[i].Title, results[i].Score, results[i].DaysSurvived, results[i].TimeTakenMs,
                    renderResults[i].Renders.Count);

            await sender.Send(new UpdateSubmissionStatusCommand(submissionId, SubmissionStatus.Succeeded), ct);
            await NotifyStatusChangeAsync(submissionId, submission.UserId, SubmissionStatus.Succeeded);
            logger.LogInformation("Submission {SubmissionId} succeeded", submissionId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Submission {SubmissionId} failed during execution", submissionId);
            var message = ex.InnerException?.Message ?? ex.Message;
            await sender.Send(new UpdateSubmissionStatusCommand(submissionId, SubmissionStatus.Failed, message), ct);
            await NotifyStatusChangeAsync(submissionId, submission.UserId, SubmissionStatus.Failed);
        }
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
            Stations = dto.Stations,
            WaterTiles = dto.WaterTiles,
            WeeklyGiftOverrides = dto.WeeklyGiftOverrides
        }
    };

    private async Task NotifyStatusChangeAsync(Guid submissionId, Guid userId, SubmissionStatus status)
    {
        try
        {
            var client = httpClientFactory.CreateClient("ApiNotify");
            await client.PostAsJsonAsync("/api/submissions/notify", new { submissionId, userId, status });
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to send SignalR notification for submission {SubmissionId}", submissionId);
        }
    }
}