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

#if ASPIRE
builder.AddServiceDefaults();
#endif

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
builder.Services.AddScoped<CodeEditorBridge>();
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
var supportedCultures = new[] { new CultureInfo("en"), new CultureInfo("nl"), new CultureInfo("ar") };
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

// Re-signs the auth cookie with the new language claim and sets the culture cookie.
// Called by the language switcher so the JWT (built from cookie claims) picks up the change.
app.MapGet("/api/auth/update-language", async (HttpContext context, string language, string? returnUrl) =>
{
    // If the user is authenticated, re-sign the cookie with the updated Language claim
    // so the JWT (built from cookie claims) picks up the change.
    var result = await context.AuthenticateAsync("BlazorServer");
    if (result.Principal is not null)
    {
        var updatedClaims = result.Principal.Claims
            .Where(c => c.Type != "Language")
            .Append(new Claim("Language", language))
            .ToList();

        var principal = new ClaimsPrincipal(new ClaimsIdentity(updatedClaims, "BlazorServer"));
        await context.SignInAsync("BlazorServer", principal, new AuthenticationProperties
        {
            IsPersistent = true,
            ExpiresUtc = DateTimeOffset.UtcNow.AddDays(30)
        });
    }

    // Always update the culture cookie so ASP.NET Core's localization middleware applies immediately.
    context.Response.Cookies.Append(
        ".AspNetCore.Culture",
        $"c={language}|uic={language}",
        new CookieOptions { MaxAge = TimeSpan.FromDays(365), Path = "/" });

    var redirect = string.IsNullOrEmpty(returnUrl) ? "/" : Uri.UnescapeDataString(returnUrl);
    return Results.Redirect(redirect);
});

// Submission render ZIP download proxy — forwards the JWT-authenticated API call through the cookie-protected Web host.
app.MapGet("/api/downloads/submissions/{submissionId:guid}/levels/{levelId:guid}/zip",
    async (Guid submissionId, Guid levelId, MetroManiaApiClient apiClient) =>
{
    var zipBytes = await apiClient.DownloadSubmissionLevelZipAsync(submissionId, levelId);
    if (zipBytes is null)
        return Results.NotFound();

    return Results.File(zipBytes, "application/zip", $"{submissionId}_{levelId}.zip");
}).RequireAuthorization();

// Individual SVG frame proxy — serves a single render frame through cookie auth.
app.MapGet("/api/renders/{submissionId:guid}/{levelId:guid}/{hour:int}.svg",
    async (Guid submissionId, Guid levelId, int hour, MetroManiaApiClient apiClient) =>
{
    var svg = await apiClient.GetSubmissionRenderFrameAsync(submissionId, levelId, hour);
    if (svg is null)
        return Results.NotFound();

    return Results.Content(svg, "image/svg+xml");
}).RequireAuthorization();

#if ASPIRE
app.MapDefaultEndpoints();
#endif
app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();