var builder = DistributedApplication.CreateBuilder(args);

// API — all environment variables come from its own launchSettings.json
var api = builder.AddProject<Projects.MetroMania_Api>("api");

// Web — depends on API; override API_BASE_URL with Aspire-managed endpoint
builder.AddProject<Projects.MetroMania_Web>("web")
    .WaitFor(api)
    .WithEnvironment("API_BASE_URL", api.GetEndpoint("https"));

// Runner Worker — depends on API for SignalR notifications
builder.AddProject<Projects.MetroMania_Worker_Runner>("runner")
    .WaitFor(api)
    .WithEnvironment("API_BASE_URL", api.GetEndpoint("https"));

// Renderer Worker — depends on API for SignalR notifications
builder.AddProject<Projects.MetroMania_Worker_Renderer>("renderer")
    .WaitFor(api)
    .WithEnvironment("API_BASE_URL", api.GetEndpoint("https"));

// Cleanup Processor — independent, processes Service Bus cleanup messages
builder.AddProject<Projects.MetroMania_CleanupProcessor>("cleanup");

builder.Build().Run();
