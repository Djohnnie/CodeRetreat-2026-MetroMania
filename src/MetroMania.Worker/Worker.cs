using System.Diagnostics;
using System.Net.Http.Json;
using System.Text.Json;
using Azure.Messaging.ServiceBus;
using MediatR;
using MetroMania.Application.DTOs;
using MetroMania.Application.Interfaces;
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
    private const int RenderBatchSize = 50;

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
        var debugBlobStorage = scope.ServiceProvider.GetRequiredService<IDebugBlobStorage>();

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

            // Phase 1: Run scripts (parallel across all levels)
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

            // Save scores immediately so the player can see them while rendering continues
            var scores = levels.Zip(results, (level, result) =>
                new SaveSubmissionScoresCommand.LevelScore(level.Id, result.Success ? result.Score : 0))
                .ToList();

            var saveScoresSw = Stopwatch.StartNew();
            await sender.Send(new SaveSubmissionScoresCommand(submissionId, scores), ct);
            saveScoresSw.Stop();
            logger.LogInformation("Saved scores for {LevelCount} levels in {ElapsedMs}ms", levels.Count, saveScoresSw.ElapsedMilliseconds);

            // Phase 2: Render scripts in batches (one level at a time to limit memory)
            await sender.Send(new UpdateSubmissionStatusCommand(submissionId, SubmissionStatus.Rendering), ct);
            await NotifyStatusChangeAsync(submissionId, submission.UserId, SubmissionStatus.Rendering);

            var renderSw = Stopwatch.StartNew();
            var renderFailures = new List<(LevelDto level, string error)>();
            var renderedLevelInfos = new List<CreateSubmissionRenderZipsCommand.LevelInfo>();

            for (int levelIndex = 0; levelIndex < levels.Count; levelIndex++)
            {
                var level = levels[levelIndex];
                logger.LogInformation("Rendering level {LevelIndex}/{LevelCount}: {LevelTitle}", levelIndex + 1, levels.Count, level.Title);

                var levelDataJson = JsonSerializer.Serialize(ToLevelEntity(level));

                var totalFrames = 0;
                var levelFailed = false;
                for (int start = 0; ; start += RenderBatchSize)
                {
                    var batchResult = await gameRendererService.RenderBatchAsync(level.Id, base64Code, levelDataJson, start, RenderBatchSize);
                    if (!batchResult.Success)
                    {
                        renderFailures.Add((level, batchResult.Error ?? "Render batch failed."));
                        levelFailed = true;
                        break;
                    }

                    totalFrames = batchResult.TotalFrames;

                    var batchRenders = batchResult.Renders
                        .Select(r => new SaveSubmissionRenderBatchCommand.LevelRender(level.Id, r.Hour, r.SvgContent, r.JsonContent))
                        .ToList();

                    await sender.Send(new SaveSubmissionRenderBatchCommand(submissionId, batchRenders), ct);

                    logger.LogInformation("Saved render batch {Start}-{End} of {Total} for level {LevelTitle}",
                        start + 1, start + batchResult.Renders.Count, totalFrames, level.Title);

                    if (start + RenderBatchSize >= totalFrames)
                        break;
                }

                if (!levelFailed)
                {
                    await sender.Send(new SaveSubmissionRenderInfoCommand(submissionId, level.Id, totalFrames), ct);
                    renderedLevelInfos.Add(new CreateSubmissionRenderZipsCommand.LevelInfo(level.Id, level.Title, totalFrames));
                }
            }

            renderSw.Stop();
            logger.LogInformation("Rendered {LevelCount} levels in {ElapsedMs}ms", levels.Count, renderSw.ElapsedMilliseconds);

            // Phase 3: Create ZIPs
            await sender.Send(new UpdateSubmissionStatusCommand(submissionId, SubmissionStatus.Finalizing), ct);
            await NotifyStatusChangeAsync(submissionId, submission.UserId, SubmissionStatus.Finalizing);

            // Create ZIP archives per level (downloads individual blobs, creates ZIP, uploads)
            if (renderedLevelInfos.Count > 0)
            {
                var zipSw = Stopwatch.StartNew();
                await sender.Send(new CreateSubmissionRenderZipsCommand(submissionId, renderedLevelInfos), ct);
                zipSw.Stop();
                logger.LogInformation("Created ZIP archives for {LevelCount} levels in {ElapsedMs}ms", renderedLevelInfos.Count, zipSw.ElapsedMilliseconds);
            }

            // Check if any level run or render failed
            var runnerFailures = levels
                .Zip(results, (level, result) => (level, result))
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

                foreach (var (level, error) in renderFailures)
                {
                    logger.LogError("Level render failed [{Level}]: {Error}", level.Title, error);
                    messageLines.Add($"[{level.Title}] Render: {error}");
                }

                var message = string.Join("\n", messageLines);
                await sender.Send(new UpdateSubmissionStatusCommand(submissionId, SubmissionStatus.Failed, message), ct);
                await NotifyStatusChangeAsync(submissionId, submission.UserId, SubmissionStatus.Failed);
                logger.LogInformation("Submission {SubmissionId} failed", submissionId);
                return;
            }

            for (var i = 0; i < levels.Count; i++)
                logger.LogInformation("Level {LevelTitle}: Score={Score}, Days={Days}, Time={Time}ms",
                    levels[i].Title, results[i].Score, results[i].DaysSurvived, results[i].TimeTakenMs);

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
            MaxDays = dto.MaxDays,
            LocalizedContent = dto.LocalizedContent,
            Stations = dto.Stations,
            WaterTiles = dto.WaterTiles,
            WeeklyGiftOverrides = dto.WeeklyGiftOverrides,
            InitialResources = dto.InitialResources
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