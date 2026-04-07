using Azure.Messaging.ServiceBus;
using MetroMania.Application.Interfaces;
using MetroMania.Application.Messages;
using Microsoft.Extensions.Configuration;

namespace MetroMania.Infrastructure.ServiceBus;

public class SubmissionQueueService : ISubmissionQueueService, IAsyncDisposable
{
    private readonly ServiceBusClient _client;
    private readonly ServiceBusSender _runSender;
    private readonly ServiceBusSender _renderSender;

    public SubmissionQueueService(IConfiguration configuration)
    {
        var connectionString = configuration.GetValue<string>("SERVICE_BUS_CONNECTION_STRING")
            ?? throw new InvalidOperationException("Set the SERVICE_BUS_CONNECTION_STRING environment variable.");
        var runQueue = configuration.GetValue<string>("SERVICE_BUS_RUN_QUEUE")
            ?? throw new InvalidOperationException("Set the SERVICE_BUS_RUN_QUEUE environment variable.");
        var renderQueue = configuration.GetValue<string>("SERVICE_BUS_RENDER_QUEUE")
            ?? throw new InvalidOperationException("Set the SERVICE_BUS_RENDER_QUEUE environment variable.");

        _client = new ServiceBusClient(connectionString);
        _runSender = _client.CreateSender(runQueue);
        _renderSender = _client.CreateSender(renderQueue);
    }

    public async Task EnqueueRunAsync(Guid submissionId, CancellationToken cancellationToken = default)
    {
        var message = new ServiceBusMessage(BinaryData.FromObjectAsJson(new ProcessSubmissionMessage(submissionId)))
        {
            ContentType = "application/json",
            Subject = "submission-run"
        };

        await _runSender.SendMessageAsync(message, cancellationToken);
    }

    public async Task EnqueueRenderAsync(Guid submissionId, CancellationToken cancellationToken = default)
    {
        var message = new ServiceBusMessage(BinaryData.FromObjectAsJson(new ProcessSubmissionRenderMessage(submissionId)))
        {
            ContentType = "application/json",
            Subject = "submission-render"
        };

        await _renderSender.SendMessageAsync(message, cancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        await _runSender.DisposeAsync();
        await _renderSender.DisposeAsync();
        await _client.DisposeAsync();
    }
}