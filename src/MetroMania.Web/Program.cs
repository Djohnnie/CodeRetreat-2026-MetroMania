using System.Globalization;
using System.Security.Claims;
using MetroMania.Web.Components;
using MetroMania.Web.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Localization;
using MudBlazor.Services;

var builder = WebApplication.CreateBuilder(args);

// API HttpClient
var apiBaseUrl = builder.Configuration.GetValue<string>("API_BASE_URL")
    ?? throw new InvalidOperationException("Configure ApiBaseUrl in appsettings.");
builder.Services.AddHttpClient<MetroManiaApiClient>(client =>
    client.BaseAddress = new Uri(apiBaseUrl));

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

// Blazor
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();

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