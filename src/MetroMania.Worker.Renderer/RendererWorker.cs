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
using MetroMania.Engine;
using MetroMania.Engine.Model;
using MetroMania.Scripting;

namespace MetroMania.Worker.Renderer;

public class RendererWorker(
    IServiceScopeFactory scopeFactory,
    IConfiguration configuration,
    IHttpClientFactory httpClientFactory,
    ILogger<RendererWorker> logger) : BackgroundService
{
    private const int RenderBatchSize = 50;

    private static readonly JsonSerializerOptions NotifyJsonOptions = new()
    {
        Converters = { new JsonStringEnumConverter() },
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private static readonly JsonSerializerOptions SnapshotJsonOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter(), new LocationJsonConverter() }
    };

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var connectionString = configuration.GetValue<string>("SERVICE_BUS_CONNECTION_STRING")
            ?? throw new InvalidOperationException("Set the SERVICE_BUS_CONNECTION_STRING environment variable.");
        var queueName = configuration.GetValue<string>("SERVICE_BUS_RENDER_QUEUE")
            ?? throw new InvalidOperationException("Set the SERVICE_BUS_RENDER_QUEUE environment variable.");

        await using var client = new ServiceBusClient(connectionString);
        await using var processor = client.CreateProcessor(queueName, new ServiceBusProcessorOptions
        {
            AutoCompleteMessages = false,
            MaxConcurrentCalls = 1
        });

        processor.ProcessMessageAsync += async args =>
        {
            var message = args.Message.Body.ToObjectFromJson<ProcessSubmissionRenderMessage>()
                ?? throw new InvalidOperationException("Failed to deserialize render message.");
            logger.LogInformation("Received render message for submission {SubmissionId}", message.SubmissionId);

            try
            {
                await ProcessRenderAsync(message.SubmissionId, stoppingToken);
                await args.CompleteMessageAsync(args.Message, stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error processing render for submission {SubmissionId}", message.SubmissionId);
                await args.AbandonMessageAsync(args.Message, cancellationToken: stoppingToken);
            }
        };

        processor.ProcessErrorAsync += args =>
        {
            logger.LogError(args.Exception, "Service Bus processing error. Source: {Source}", args.ErrorSource);
            return Task.CompletedTask;
        };

        await processor.StartProcessingAsync(stoppingToken);
        logger.LogInformation("Renderer worker started, listening on queue '{Queue}'", queueName);

        await Task.Delay(Timeout.Infinite, stoppingToken);

        await processor.StopProcessingAsync(stoppingToken);
    }

    private async Task ProcessRenderAsync(Guid submissionId, CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();

        var submission = await sender.Send(new GetSubmissionByIdQuery(submissionId), ct)
            ?? throw new InvalidOperationException($"Submission {submissionId} not found.");

        var levels = await sender.Send(new GetAllLevelsQuery(), ct);
        if (levels.Count == 0)
        {
            logger.LogWarning("No levels found, skipping render for submission {SubmissionId}", submissionId);
            return;
        }

        // Mark as rendering
        await sender.Send(new UpdateSubmissionStatusCommand(submissionId,
            RenderStatus: RenderStatus.Rendering), ct);
        await NotifyStatusChangeAsync(submissionId, submission.UserId, null, RenderStatus.Rendering);

        logger.LogInformation("Rendering submission {SubmissionId} for {LevelCount} levels",
            submissionId, levels.Count);

        try
        {
            var base64Code = submission.Code.Base64Encode();
            var svgResourcesPath = ResolveSvgResourcesPath();

            var renderFailures = new List<(LevelDto level, string error)>();
            var renderedLevelInfos = new List<CreateSubmissionRenderZipsCommand.LevelInfo>();

            for (int levelIndex = 0; levelIndex < levels.Count; levelIndex++)
            {
                var level = levels[levelIndex];
                logger.LogInformation("Rendering level {LevelIndex}/{LevelCount}: {LevelTitle}",
                    levelIndex + 1, levels.Count, level.Title);

                var levelEntity = ToLevelEntity(level);
                GameResult? gameResult;

                try
                {
                    gameResult = await RunSimulationWithSnapshots(base64Code, levelEntity);
                    if (gameResult is null)
                    {
                        renderFailures.Add((level, "Script returned null."));
                        continue;
                    }
                }
                catch (Exception ex)
                {
                    renderFailures.Add((level, ex.InnerException?.Message ?? ex.Message));
                    continue;
                }

                var snapshots = gameResult.GameSnapshots;
                var totalFrames = snapshots.Count;

                using var renderer = new MetroManiaRenderer(svgResourcesPath);

                // Render in batches
                for (int start = 0; start < totalFrames; start += RenderBatchSize)
                {
                    var end = Math.Min(start + RenderBatchSize, totalFrames);
                    var batchRenders = new List<SaveSubmissionRenderBatchCommand.LevelRender>();

                    for (int i = start; i < end; i++)
                    {
                        var svg = renderer.RenderSnapshot(levelEntity, snapshots[i]);
                        var json = JsonSerializer.Serialize(snapshots[i], SnapshotJsonOptions);
                        batchRenders.Add(new SaveSubmissionRenderBatchCommand.LevelRender(
                            level.Id, i + 1, svg, json));
                    }

                    await sender.Send(new SaveSubmissionRenderBatchCommand(submissionId, batchRenders), ct);

                    logger.LogInformation("Saved render batch {Start}-{End} of {Total} for level {LevelTitle}",
                        start + 1, end, totalFrames, level.Title);
                }

                await sender.Send(new SaveSubmissionRenderInfoCommand(submissionId, level.Id, totalFrames), ct);
                renderedLevelInfos.Add(new CreateSubmissionRenderZipsCommand.LevelInfo(
                    level.Id, level.Title, totalFrames));
            }

            // Create ZIP archives
            if (renderedLevelInfos.Count > 0)
            {
                var zipSw = Stopwatch.StartNew();
                await sender.Send(new CreateSubmissionRenderZipsCommand(submissionId, renderedLevelInfos), ct);
                zipSw.Stop();
                logger.LogInformation("Created ZIP archives for {LevelCount} levels in {ElapsedMs}ms",
                    renderedLevelInfos.Count, zipSw.ElapsedMilliseconds);
            }

            if (renderFailures.Count > 0)
            {
                var message = string.Join("\n", renderFailures.Select(f =>
                    $"[{f.level.Title}] Render: {f.error}"));

                await sender.Send(new UpdateSubmissionStatusCommand(submissionId,
                    RenderStatus: RenderStatus.Failed, Message: message), ct);
                await NotifyStatusChangeAsync(submissionId, submission.UserId, null, RenderStatus.Failed);
                return;
            }

            // Mark render as completed
            await sender.Send(new UpdateSubmissionStatusCommand(submissionId,
                RenderStatus: RenderStatus.Rendered), ct);
            await NotifyStatusChangeAsync(submissionId, submission.UserId, null, RenderStatus.Rendered);

            logger.LogInformation("Submission {SubmissionId} render completed", submissionId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Submission {SubmissionId} render failed", submissionId);
            var message = ex.InnerException?.Message ?? ex.Message;
            await sender.Send(new UpdateSubmissionStatusCommand(submissionId,
                RenderStatus: RenderStatus.Failed, Message: message), ct);
            await NotifyStatusChangeAsync(submissionId, submission.UserId, null, RenderStatus.Failed);
        }
    }

    private static async Task<GameResult?> RunSimulationWithSnapshots(string base64Code, Level level)
    {
        var wrappedScript = WrapInRenderScript(base64Code);
        var scriptCompiler = new ScriptCompiler<GameResult>();

        var script = await scriptCompiler.CompileForExecution(wrappedScript);
        return await script.Invoke(new ScriptGlobals(level));
    }

    private static string WrapInRenderScript(string base64PlayerCode)
    {
        var playerCode = base64PlayerCode.Base64Decode();

        var outerScript = """
            var engine = new MetroManiaEngine();
            var runner = new MyMetroManiaRunner();
            var result = engine.Run(runner, Level, maxHours: Level.LevelData.MaxDays * 24);
            return result;

            <<PLACEHOLDER>>
            """;

        return outerScript.Replace("<<PLACEHOLDER>>", playerCode).Base64Encode();
    }

    private string ResolveSvgResourcesPath()
    {
        var configured = configuration.GetValue<string>("SVG_RESOURCES_PATH");
        if (!string.IsNullOrWhiteSpace(configured))
            return configured;

        var dockerPath = Path.Combine(AppContext.BaseDirectory, "resources");
        if (Directory.Exists(dockerPath))
            return dockerPath;

        return Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "resources"));
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
