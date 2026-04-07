using MetroMania.Application.Interfaces;
using MetroMania.Infrastructure.BlobStorage;
using MetroMania.Infrastructure.Sql;
using MetroMania.Worker.Renderer;

var builder = Host.CreateApplicationBuilder(args);

#if ASPIRE
builder.AddServiceDefaults();
#endif

// Infrastructure (EF Core, repositories)
var connectionString = Environment.GetEnvironmentVariable("SQL_CONNECTION_STRING")
    ?? builder.Configuration.GetConnectionString("Default")
    ?? throw new InvalidOperationException("Set the SQL_CONNECTION_STRING environment variable or configure ConnectionStrings:Default.");
builder.Services.AddInfrastructure(connectionString);

// Blob Storage (renders)
var blobStorageConnectionString = Environment.GetEnvironmentVariable("AZURE_STORAGE_CONNECTION_STRING")
    ?? builder.Configuration.GetValue<string>("AZURE_STORAGE_CONNECTION_STRING")
    ?? throw new InvalidOperationException("Set the AZURE_STORAGE_CONNECTION_STRING environment variable.");
builder.Services.AddBlobStorage(blobStorageConnectionString);

// MediatR
builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssembly(typeof(MetroMania.Application.DTOs.UserDto).Assembly));

// Stub registrations for services this worker doesn't use but MediatR discovers handlers for
builder.Services.AddSingleton<IScriptValidationService, NullScriptValidationService>();
builder.Services.AddSingleton<ICleanupQueueService, NullCleanupQueueService>();
builder.Services.AddSingleton<ISubmissionQueueService, NullSubmissionQueueService>();

// HttpClient for API SignalR notifications
var apiBaseUrl = Environment.GetEnvironmentVariable("API_BASE_URL") ?? "https://localhost:5101";
builder.Services.AddHttpClient("ApiNotify", client => client.BaseAddress = new Uri(apiBaseUrl));

builder.Services.AddHostedService<RendererWorker>();

var host = builder.Build();
host.Run();

// Stubs for unused services — MediatR scans the whole Application assembly
file class NullScriptValidationService : IScriptValidationService
{
    public Task<ScriptValidationResult> ValidateAsync(string base64Code) => throw new NotSupportedException();
}

file class NullCleanupQueueService : ICleanupQueueService
{
    public Task EnqueueCleanupAsync(Guid submissionId, List<(Guid LevelId, int TotalFrames)> renders, CancellationToken cancellationToken = default) => throw new NotSupportedException();
}

file class NullSubmissionQueueService : ISubmissionQueueService
{
    public Task EnqueueRunAsync(Guid submissionId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    public Task EnqueueRenderAsync(Guid submissionId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
}
