using System.Globalization;
using System.Security.Claims;
using MetroMania.Web.Components;
using MetroMania.Web.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.Http.Resilience;
using MudBlazor.Services;
using Polly;

var builder = WebApplication.CreateBuilder(args);

// API HttpClient
var apiBaseUrl = builder.Configuration.GetValue<string>("API_BASE_URL")
    ?? throw new InvalidOperationException("Configure ApiBaseUrl in appsettings.");
builder.Services.AddHttpClient<MetroManiaApiClient>(client =>
    client.BaseAddress = new Uri(apiBaseUrl))
    .AddResilienceHandler("api-retry", pipeline =>
    {
        pipeline.AddRetry(new HttpRetryStrategyOptions
        {
            MaxRetryAttempts = 5,
            BackoffType = DelayBackoffType.Exponential,
            Delay = TimeSpan.FromSeconds(2),
            ShouldHandle = args => ValueTask.FromResult(
                args.Outcome.Exception is HttpRequestException or TaskCanceledException),
            OnRetry = args =>
            {
                var logger = args.Context.Properties.GetValue(
                    new ResiliencePropertyKey<ILogger>("logger"), null!);
                logger?.LogWarning(
                    "API request failed ({Exception}). Retry {Attempt} in {Delay}...",
                    args.Outcome.Exception?.GetType().Name ?? "unknown",
                    args.AttemptNumber + 1,
                    args.RetryDelay);
                return ValueTask.CompletedTask;
            }
        });
    });

// MudBlazor
builder.Services.AddMudServices();

// Localization
builder.Services.AddLocalization();

// Auth
builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton<LoginTicketService>();
builder.Services.AddScoped<JwtTokenProvider>();
builder.Services.AddAuthentication("BlazorServer")
    .AddCookie("BlazorServer", options =>
    {
        options.LoginPath = "/login";
        options.ExpireTimeSpan = TimeSpan.FromDays(30);
        options.SlidingExpiration = true;
    });
builder.Services.AddAuthorization();
builder.Services.AddScoped<CookieAuthStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(sp =>
    sp.GetRequiredService<CookieAuthStateProvider>());

// Data Protection — persist keys to a stable path so antiforgery tokens survive container restarts.
// Set DATA_PROTECTION_KEYS as an env var containing the XML key ring content.
var dpKeysDir = Path.Combine(Path.GetTempPath(), "dataprotection-keys");
Directory.CreateDirectory(dpKeysDir);

var dpKeysEnv = Environment.GetEnvironmentVariable("DATA_PROTECTION_KEYS");
if (!string.IsNullOrEmpty(dpKeysEnv))
{
    var keyFilePath = Path.Combine(dpKeysDir, "key-from-env.xml");
    await File.WriteAllTextAsync(keyFilePath, dpKeysEnv);
}

builder.Services.AddDataProtection()
    .SetApplicationName("MetroMania")
    .PersistKeysToFileSystem(new DirectoryInfo(dpKeysDir));

// Blazor
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
}

// Behind a reverse proxy (Azure Container Apps), trust forwarded headers so the app
// sees the original HTTPS scheme. Required for secure cookies, correct redirects, and wss:// WebSockets.
// Clear KnownProxies/KnownNetworks to trust the ACA Envoy proxy (which isn't on localhost).
var forwardedHeadersOptions = new ForwardedHeadersOptions
{
    ForwardedHeaders = Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedFor
                     | Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedProto
};
forwardedHeadersOptions.KnownProxies.Clear();
forwardedHeadersOptions.KnownIPNetworks.Clear();
app.UseForwardedHeaders(forwardedHeadersOptions);

app.UseHttpsRedirection();

// Culture middleware
var supportedCultures = new[] { new CultureInfo("en"), new CultureInfo("nl") };
app.UseRequestLocalization(new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture("en"),
    SupportedCultures = supportedCultures,
    SupportedUICultures = supportedCultures
});

app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

// Auth callback endpoints (cookie management must stay in the Web host)
app.MapGet("/api/auth/login-callback", async (HttpContext context, string ticket,
    LoginTicketService ticketService) =>
{
    var user = ticketService.RedeemTicket(ticket);
    if (user is null)
        return Results.Redirect("/login");

    var claims = new List<Claim>
    {
        new(ClaimTypes.NameIdentifier, user.Id.ToString()),
        new(ClaimTypes.Name, user.Name),
        new(ClaimTypes.Role, user.Role.ToString()),
        new("IsDarkMode", user.IsDarkMode.ToString()),
        new("Language", user.Language)
    };
    var identity = new ClaimsIdentity(claims, "BlazorServer");
    var principal = new ClaimsPrincipal(identity);

    await context.SignInAsync("BlazorServer", principal, new AuthenticationProperties
    {
        IsPersistent = true,
        ExpiresUtc = DateTimeOffset.UtcNow.AddDays(30)
    });

    return Results.Redirect("/");
});

app.MapGet("/api/auth/logout", async (HttpContext context) =>
{
    await context.SignOutAsync("BlazorServer");
    return Results.Redirect("/login");
});

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();