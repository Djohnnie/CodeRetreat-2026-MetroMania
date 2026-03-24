using System.Security.Claims;
using MetroMania.Domain.Enums;
using MetroMania.Domain.Interfaces;
using Microsoft.AspNetCore.Components.Authorization;

namespace MetroMania.Web.Services;

public class CookieAuthStateProvider : AuthenticationStateProvider
{
    private readonly ClaimsPrincipal? _initialHttpUser;
    private readonly IServiceProvider _serviceProvider;

    public CookieAuthStateProvider(IHttpContextAccessor httpContextAccessor, IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        _initialHttpUser = httpContextAccessor.HttpContext?.User;
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        if (_initialHttpUser?.Identity?.IsAuthenticated == true)
        {
            var userIdStr = _initialHttpUser.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (Guid.TryParse(userIdStr, out var userId))
            {
                using var scope = _serviceProvider.CreateScope();
                var userRepo = scope.ServiceProvider.GetRequiredService<IUserRepository>();
                var user = await userRepo.GetByIdAsync(userId);

                if (user is not null && user.ApprovalStatus == ApprovalStatus.Approved)
                {
                    var claims = new List<Claim>
                    {
                        new(ClaimTypes.NameIdentifier, user.Id.ToString()),
                        new(ClaimTypes.Name, user.Name),
                        new(ClaimTypes.Role, user.Role.ToString()),
                        new("IsDarkMode", user.IsDarkMode.ToString()),
                        new("Language", user.Language)
                    };
                    var identity = new ClaimsIdentity(claims, "BlazorServer");
                    return new AuthenticationState(new ClaimsPrincipal(identity));
                }
            }
        }

        return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
    }
}
