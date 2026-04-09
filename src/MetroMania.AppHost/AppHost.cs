var builder = DistributedApplication.CreateBuilder(args);

// Orleans Validation Silo — isolated process for script validation (protects the API from bad scripts)
var validationSilo = builder.AddProject<Projects.MetroMania_Orleans_Validation>("validation")
    .WithUrl("/dashboard", "Orleans Dashboard");

// API — all environment variables come from its own launchSettings.json
var api = builder.AddProject<Projects.MetroMania_Api>("api")
    .WaitFor(validationSilo)
    .WithUrlForEndpoint("https", url => url.DisplayText = "MetroMania API");

// Web — depends on API; override API_BASE_URL with Aspire-managed endpoint
builder.AddProject<Projects.MetroMania_Web>("web")
    .WaitFor(api)
    .WithEnvironment("API_BASE_URL", api.GetEndpoint("https"))
    .WithUrlForEndpoint("https", url => url.DisplayText = "MetroMania Blazor Web App");

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
