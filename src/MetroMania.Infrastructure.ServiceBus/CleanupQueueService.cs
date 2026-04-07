using Azure.Messaging.ServiceBus;
using MetroMania.Application.Interfaces;
using MetroMania.Application.Messages;
using Microsoft.Extensions.Configuration;

namespace MetroMania.Infrastructure.ServiceBus;

public class CleanupQueueService : ICleanupQueueService, IAsyncDisposable
{
    private readonly ServiceBusClient _client;
    private readonly ServiceBusSender _sender;

    public CleanupQueueService(IConfiguration configuration)
    {
        var connectionString = configuration.GetValue<string>("SERVICE_BUS_CONNECTION_STRING")
            ?? throw new InvalidOperationException("Set the SERVICE_BUS_CONNECTION_STRING environment variable.");
        var queueName = configuration.GetValue<string>("SERVICE_BUS_CLEANUP_QUEUE")
            ?? throw new InvalidOperationException("Set the SERVICE_BUS_CLEANUP_QUEUE environment variable.");

        _client = new ServiceBusClient(connectionString);
        _sender = _client.CreateSender(queueName);
    }

    public async Task EnqueueCleanupAsync(Guid submissionId, List<(Guid LevelId, int TotalFrames)> renders, CancellationToken cancellationToken = default)
    {
        var payload = new CleanupSubmissionMessage(
            submissionId,
            renders.Select(r => new CleanupSubmissionMessage.RenderInfo(r.LevelId, r.TotalFrames)).ToList());

        var message = new ServiceBusMessage(BinaryData.FromObjectAsJson(payload))
        {
            ContentType = "application/json",
            Subject = "submission-cleanup"
        };

        await _sender.SendMessageAsync(message, cancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        await _sender.DisposeAsync();
        await _client.DisposeAsync();
    }
}
