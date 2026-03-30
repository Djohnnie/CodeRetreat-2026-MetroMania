using Azure.Messaging.ServiceBus;
using MetroMania.Application.Messages;
using MetroMania.Infrastructure.Orleans.Services;

namespace MetroMania.Worker;

public class ServiceBusWorker(
    IGameEngineService gameEngineService,
    IConfiguration configuration,
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
            var message = args.Message.Body.ToObjectFromJson<ProcessSubmissionMessage>();
            logger.LogInformation("Received submission {SubmissionId}", message.SubmissionId);

            try
            {
                var result = await gameEngineService.PingAsync(Guid.NewGuid());
                logger.LogInformation("GameEngineService ping result: {Result}", result);

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
}