using Azure.Data.Tables;
using MetroMania.Infrastructure.Orleans;
using MetroMania.Infrastructure.ServiceBus;
using MetroMania.Infrastructure.Sql;
using MetroMania.Worker;
using Orleans.Configuration;

var builder = Host.CreateApplicationBuilder(args);

// Infrastructure (EF Core, repositories)
var connectionString = Environment.GetEnvironmentVariable("SQL_CONNECTION_STRING")
    ?? builder.Configuration.GetConnectionString("Default")
    ?? throw new InvalidOperationException("Set the SQL_CONNECTION_STRING environment variable or configure ConnectionStrings:Default.");
builder.Services.AddInfrastructure(connectionString);

// Service Bus
builder.Services.AddServiceBus();

// MediatR
builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssembly(typeof(MetroMania.Application.DTOs.UserDto).Assembly));

// Orleans client — connects to the Orleans cluster
builder.UseOrleansClient(clientBuilder =>
{
    var azureStorageConnectionString = clientBuilder.Configuration.GetValue<string>("AZURE_STORAGE_CONNECTION_STRING");

#if DEBUG
    clientBuilder.UseLocalhostClustering();
#else
    clientBuilder.Configure<ClusterOptions>(options =>
    {
        options.ClusterId = "metromania-orleans";
        options.ServiceId = "metromania-orleans";
    });

    clientBuilder.UseAzureStorageClustering(options =>
    {
        options.TableServiceClient = new TableServiceClient(azureStorageConnectionString);
    });
#endif

    clientBuilder
        .Configure<ClientMessagingOptions>(options =>
        {
            options.ResponseTimeout = TimeSpan.FromMinutes(5);
        });
});
builder.Services.AddOrleansClient();

// HttpClient for API SignalR notifications
var apiBaseUrl = Environment.GetEnvironmentVariable("API_BASE_URL") ?? "https://localhost:5101";
builder.Services.AddHttpClient("ApiNotify", client => client.BaseAddress = new Uri(apiBaseUrl));

builder.Services.AddHostedService<ServiceBusWorker>();

var host = builder.Build();
host.Run();
