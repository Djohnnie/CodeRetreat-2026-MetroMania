using System.Globalization;
using System.Security.Claims;
using MetroMania.Domain.Enums;
using MetroMania.Domain.Interfaces;
using MetroMania.Infrastructure;
using MetroMania.Infrastructure.Persistence;
using MetroMania.Web.Components;
using MetroMania.Web.Services;
using Microsoft.AspNetCore.Authentication;
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
builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton<LoginTicketService>();
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

app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

// Auth endpoints
app.MapGet("/api/auth/login-callback", async (HttpContext context, string ticket,
    LoginTicketService ticketService, IServiceProvider sp) =>
{
    var userId = ticketService.RedeemTicket(ticket);
    if (userId is null)
        return Results.Redirect("/login");

    using var scope = sp.CreateScope();
    var userRepo = scope.ServiceProvider.GetRequiredService<IUserRepository>();
    var user = await userRepo.GetByIdAsync(userId.Value);

    if (user is null || user.ApprovalStatus != ApprovalStatus.Approved)
        return Results.Redirect("/login");

    var claims = new List<Claim>
    {
        new(ClaimTypes.NameIdentifier, user.Id.ToString()),
        new(ClaimTypes.Name, user.Name),
        new(ClaimTypes.Role, user.Role.ToString())
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
