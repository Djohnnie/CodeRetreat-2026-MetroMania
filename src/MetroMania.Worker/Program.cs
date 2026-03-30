using MetroMania.Infrastructure.Orleans;
using MetroMania.Worker;

var builder = Host.CreateApplicationBuilder(args);

builder.UseOrleansClient(clientBuilder =>
    clientBuilder.UseLocalhostClustering());
builder.Services.AddOrleansClient();

builder.Services.AddHostedService<ServiceBusWorker>();

var host = builder.Build();
host.Run();
