using MetroMania.Api.Endpoints;
using MetroMania.Api.Hubs;
using MetroMania.Application.Interfaces;
using MetroMania.Application.Services;
using MetroMania.Infrastructure.AzureOpenAI;
using MetroMania.Infrastructure.BlobStorage;
using MetroMania.Infrastructure.ServiceBus;
using MetroMania.Infrastructure.Sql;
using MetroMania.Infrastructure.Sql.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Infrastructure (EF Core, repositories, password hasher)
var connectionString = Environment.GetEnvironmentVariable("SQL_CONNECTION_STRING")
    ?? builder.Configuration.GetConnectionString("Default")
    ?? throw new InvalidOperationException("Set the SQL_CONNECTION_STRING environment variable or configure ConnectionStrings:Default.");
builder.Services.AddInfrastructure(connectionString);

// Blob Storage (renders)
var blobStorageConnectionString = Environment.GetEnvironmentVariable("AZURE_STORAGE_CONNECTION_STRING")
    ?? builder.Configuration.GetValue<string>("AZURE_STORAGE_CONNECTION_STRING")
    ?? throw new InvalidOperationException("Set the AZURE_STORAGE_CONNECTION_STRING environment variable.");
builder.Services.AddBlobStorage(blobStorageConnectionString);

// Script validation — runs directly in the API process via Roslyn
builder.Services.AddSingleton<IScriptValidationService, ScriptValidationService>();

// Service Bus
builder.Services.AddServiceBus();

// Azure OpenAI — Conductor chatbot agent
// Instructions are loaded from conductor-instructions.md in the Web project's wwwroot.
// Dev: two levels up from the API content root → src/MetroMania.Web/wwwroot
// Docker: falls back to conductor-instructions.md next to the API binary
var webWwwroot = Path.GetFullPath(Path.Combine(builder.Environment.ContentRootPath, "..", "MetroMania.Web", "wwwroot"));
var conductorInstructionsPath = Path.Combine(webWwwroot, "conductor-instructions.md");
if (!File.Exists(conductorInstructionsPath))
    conductorInstructionsPath = Path.Combine(builder.Environment.ContentRootPath, "conductor-instructions.md");
builder.Services.AddAzureOpenAIConductor(builder.Configuration, conductorInstructionsPath);

// MediatR
builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssembly(typeof(MetroMania.Application.DTOs.UserDto).Assembly));

// JSON serialization with string enums
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

// JWT Bearer Authentication
var jwtSecretKey = builder.Configuration["Jwt:SecretKey"]
    ?? throw new InvalidOperationException("Jwt:SecretKey is not configured.");
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        // Map inbound claims so "role" → ClaimTypes.Role (needed for RequireRole)
        options.MapInboundClaims = true;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecretKey))
        };
    });
builder.Services.AddAuthorizationBuilder()
    .AddPolicy("Admin", policy => policy.RequireRole("Admin"));

// SignalR for real-time submission status updates
builder.Services.AddSignalR();

// CORS — allow the Blazor Web project and Worker to call this API
builder.Services.AddCors(options =>
    options.AddDefaultPolicy(policy =>
        policy.SetIsOriginAllowed(_ => true).AllowAnyMethod().AllowAnyHeader().AllowCredentials()));

var app = builder.Build();

// Auto-migrate database
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
}

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

app.MapDefaultEndpoints();

// SignalR hub for real-time submission notifications
app.MapHub<SubmissionHub>("/hubs/submissions");

// Map all domain endpoints
app.MapAuthEndpoints();
app.MapUserEndpoints();
app.MapLevelEndpoints();
app.MapSubmissionEndpoints();
app.MapLeaderboardEndpoints();
app.MapThemeEndpoints();
app.MapLanguageEndpoints();
app.MapConductorEndpoints();
app.MapTranslationEndpoints();

app.Run();
