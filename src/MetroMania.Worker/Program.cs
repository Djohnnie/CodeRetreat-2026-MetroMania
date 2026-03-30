using MetroMania.Infrastructure.Orleans;
using MetroMania.Infrastructure.ServiceBus;
using MetroMania.Infrastructure.Sql;
using MetroMania.Worker;

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

// Orleans client
builder.UseOrleansClient(clientBuilder =>
    clientBuilder.UseLocalhostClustering());
builder.Services.AddOrleansClient();

builder.Services.AddHostedService<ServiceBusWorker>();

var host = builder.Build();
host.Run();
