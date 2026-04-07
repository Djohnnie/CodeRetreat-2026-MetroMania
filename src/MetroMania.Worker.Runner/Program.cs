using MetroMania.Application.Interfaces;
using MetroMania.Infrastructure.Sql;
using MetroMania.Worker.Runner;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();

// Infrastructure (EF Core, repositories)
var connectionString = Environment.GetEnvironmentVariable("SQL_CONNECTION_STRING")
    ?? builder.Configuration.GetConnectionString("Default")
    ?? throw new InvalidOperationException("Set the SQL_CONNECTION_STRING environment variable or configure ConnectionStrings:Default.");
builder.Services.AddInfrastructure(connectionString);

// MediatR
builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssembly(typeof(MetroMania.Application.DTOs.UserDto).Assembly));

// Stub registrations for services this worker doesn't use but MediatR discovers handlers for
builder.Services.AddSingleton<IRenderBlobStorage, NullRenderBlobStorage>();
builder.Services.AddSingleton<IScriptValidationService, NullScriptValidationService>();
builder.Services.AddSingleton<ICleanupQueueService, NullCleanupQueueService>();
builder.Services.AddSingleton<ISubmissionQueueService, NullSubmissionQueueService>();

// HttpClient for API SignalR notifications
var apiBaseUrl = Environment.GetEnvironmentVariable("API_BASE_URL") ?? "https://localhost:5101";
builder.Services.AddHttpClient("ApiNotify", client => client.BaseAddress = new Uri(apiBaseUrl));

builder.Services.AddHostedService<RunnerWorker>();

var host = builder.Build();
host.Run();

// Stubs for unused services — MediatR scans the whole Application assembly
file class NullRenderBlobStorage : IRenderBlobStorage
{
    public Task UploadAsync(string blobName, string content, CancellationToken ct = default) => throw new NotSupportedException();
    public Task UploadBytesAsync(string blobName, byte[] content, string contentType, CancellationToken ct = default) => throw new NotSupportedException();
    public Task<string> DownloadAsync(string blobName, CancellationToken ct = default) => throw new NotSupportedException();
    public Task<byte[]> DownloadBytesAsync(string blobName, CancellationToken ct = default) => throw new NotSupportedException();
    public Task DeleteAsync(string blobName, CancellationToken ct = default) => throw new NotSupportedException();
}

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
