using System.Text.Json;
using Azure.Messaging.ServiceBus;
using Azure.Storage.Blobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

var configuration = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json", optional: true)
    .AddEnvironmentVariables()
    .Build();

using var loggerFactory = LoggerFactory.Create(b => b.AddConsole());
var logger = loggerFactory.CreateLogger("CleanupProcessor");

var serviceBusConnectionString = configuration.GetValue<string>("SERVICE_BUS_CONNECTION_STRING")
    ?? throw new InvalidOperationException("Set the SERVICE_BUS_CONNECTION_STRING environment variable.");
var queueName = configuration.GetValue<string>("SERVICE_BUS_CLEANUP_QUEUE")
    ?? throw new InvalidOperationException("Set the SERVICE_BUS_CLEANUP_QUEUE environment variable.");
var storageConnectionString = configuration.GetValue<string>("AZURE_STORAGE_CONNECTION_STRING")
    ?? throw new InvalidOperationException("Set the AZURE_STORAGE_CONNECTION_STRING environment variable.");

await using var serviceBusClient = new ServiceBusClient(serviceBusConnectionString);
await using var receiver = serviceBusClient.CreateReceiver(queueName, new ServiceBusReceiverOptions
{
    ReceiveMode = ServiceBusReceiveMode.PeekLock
});

logger.LogInformation("Waiting for cleanup message on queue '{Queue}'...", queueName);

var message = await receiver.ReceiveMessageAsync(TimeSpan.FromSeconds(30));
if (message is null)
{
    logger.LogInformation("No message received within timeout. Exiting.");
    return;
}

try
{
    var payload = message.Body.ToObjectFromJson<CleanupMessage>(new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
        ?? throw new InvalidOperationException("Failed to deserialize cleanup message.");

    logger.LogInformation("Processing cleanup for submission {SubmissionId} with {LevelCount} levels",
        payload.SubmissionId, payload.Renders.Count);

    var blobServiceClient = new BlobServiceClient(storageConnectionString);
    var containerClient = blobServiceClient.GetBlobContainerClient("submission-renders");

    var blobNames = new List<string>();
    foreach (var render in payload.Renders)
    {
        for (var hour = 1; hour <= render.TotalFrames; hour++)
        {
            blobNames.Add($"{payload.SubmissionId}_{render.LevelId}_{hour:D4}.svgz");
            blobNames.Add($"{payload.SubmissionId}_{render.LevelId}_{hour:D4}.json");
        }
        blobNames.Add($"{payload.SubmissionId}_{render.LevelId}.zip");
    }

    logger.LogInformation("Deleting {BlobCount} blobs for submission {SubmissionId}", blobNames.Count, payload.SubmissionId);

    var deleteTasks = blobNames.Select(async name =>
    {
        try
        {
            await containerClient.DeleteBlobIfExistsAsync(name);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to delete blob {BlobName}", name);
        }
    });
    await Task.WhenAll(deleteTasks);

    await receiver.CompleteMessageAsync(message);
    logger.LogInformation("Cleanup completed for submission {SubmissionId}", payload.SubmissionId);
}
catch (Exception ex)
{
    logger.LogError(ex, "Error processing cleanup message");
    await receiver.AbandonMessageAsync(message);
    throw;
}

record CleanupMessage(Guid SubmissionId, List<CleanupMessage.RenderInfo> Renders)
{
    public record RenderInfo(Guid LevelId, int TotalFrames);
}
