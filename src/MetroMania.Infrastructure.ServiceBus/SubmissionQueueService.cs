using Azure.Messaging.ServiceBus;
using MetroMania.Application.Interfaces;
using MetroMania.Application.Messages;
using Microsoft.Extensions.Configuration;

namespace MetroMania.Infrastructure.ServiceBus;

public class SubmissionQueueService : ISubmissionQueueService, IAsyncDisposable
{
    private readonly ServiceBusClient _client;
    private readonly ServiceBusSender _sender;

    public SubmissionQueueService(IConfiguration configuration)
    {
        var connectionString = configuration.GetValue<string>("SERVICE_BUS_CONNECTION_STRING")
            ?? throw new InvalidOperationException("Set the SERVICE_BUS_CONNECTION_STRING environment variable.");
        var queueName = configuration.GetValue<string>("SERVICE_BUS_QUEUE")
            ?? throw new InvalidOperationException("Set the SERVICE_BUS_QUEUE environment variable.");

        _client = new ServiceBusClient(connectionString);
        _sender = _client.CreateSender(queueName);
    }

    public async Task EnqueueSubmissionAsync(Guid submissionId, CancellationToken cancellationToken = default)
    {
        var message = new ServiceBusMessage(BinaryData.FromObjectAsJson(new ProcessSubmissionMessage(submissionId)))
        {
            ContentType = "application/json",
            Subject = "submission-processing"
        };

        await _sender.SendMessageAsync(message, cancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        await _sender.DisposeAsync();
        await _client.DisposeAsync();
    }
}