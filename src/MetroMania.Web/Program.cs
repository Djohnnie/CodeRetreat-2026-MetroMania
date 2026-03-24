using System.Globalization;
using MetroMania.Infrastructure;
using MetroMania.Infrastructure.Persistence;
using MetroMania.Web.Components;
using MetroMania.Web.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using MudBlazor.Services;

var builder = WebApplication.CreateBuilder(args);

// Infrastructure (EF Core, repositories, password hasher)
var connectionString = Environment.GetEnvironmentVariable("SQL_CONNECTION_STRING")
    ?? builder.Configuration.GetConnectionString("Default")
    ?? throw new InvalidOperationException("Set the SQL_CONNECTION_STRING environment variable or configure ConnectionStrings:Default.");
builder.Services.AddInfrastructure(connectionString);

// MediatR
builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssembly(typeof(MetroMania.Application.DTOs.UserDto).Assembly));

// MudBlazor
builder.Services.AddMudServices();

// Localization
builder.Services.AddLocalization();

// Auth
builder.Services.AddAuthorizationCore();
builder.Services.AddScoped<CookieAuthStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(sp =>
    sp.GetRequiredService<CookieAuthStateProvider>());

// Blazor
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();

// Auto-migrate database
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();

// Culture middleware
var supportedCultures = new[] { new CultureInfo("en"), new CultureInfo("nl") };
app.UseRequestLocalization(new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture("en"),
    SupportedCultures = supportedCultures,
    SupportedUICultures = supportedCultures
});

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
