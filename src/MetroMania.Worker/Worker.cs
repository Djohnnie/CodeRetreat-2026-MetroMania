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
            // Run the script against each level in parallel via Orleans grains
            var tasks = levels.Select(level =>
            {
                logger.LogInformation("Running script for level {LevelId} ({LevelTitle})", level.Id, level.Title);
                var levelEntity = ToLevelEntity(level);
                var levelDataJson = JsonSerializer.Serialize(levelEntity);
                var base64Code = submission.Code.Base64Encode();
                return gameRunnerService.RunScriptAsync(level.Id, base64Code, levelDataJson);
            }).ToList();

            var results = await Task.WhenAll(tasks);

            // Build scores from results (score = 0 for failed levels)
            var scores = levels.Zip(results, (level, result) =>
                new SaveSubmissionScoresCommand.LevelScore(level.Id, result.Success ? result.Score : 0))
                .ToList();

            await sender.Send(new SaveSubmissionScoresCommand(submissionId, scores), ct);

            // Check if any level run failed
            var failures = results.Where(r => !r.Success).ToList();
            if (failures.Count > 0)
            {
                foreach (var failure in failures)
                    logger.LogError("Level run failed: {Error}", failure.Error);

                await sender.Send(new UpdateSubmissionStatusCommand(submissionId, SubmissionStatus.Failed), ct);
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
            await sender.Send(new UpdateSubmissionStatusCommand(submissionId, SubmissionStatus.Failed), ct);
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