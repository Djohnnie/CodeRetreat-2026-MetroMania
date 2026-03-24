using System.Security.Claims;
using MetroMania.Domain.Enums;
using MetroMania.Domain.Interfaces;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;

namespace MetroMania.Web.Services;

public class CookieAuthStateProvider(ProtectedSessionStorage sessionStorage, IServiceProvider serviceProvider)
    : AuthenticationStateProvider
{
    private ClaimsPrincipal _currentUser = new(new ClaimsIdentity());

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        try
        {
            var result = await sessionStorage.GetAsync<string>("userId");
            if (result.Success && Guid.TryParse(result.Value, out var userId))
            {
                using var scope = serviceProvider.CreateScope();
                var userRepo = scope.ServiceProvider.GetRequiredService<IUserRepository>();
                var user = await userRepo.GetByIdAsync(userId);

                if (user is not null && user.ApprovalStatus == ApprovalStatus.Approved)
                {
                    var claims = new List<Claim>
                    {
                        new(ClaimTypes.NameIdentifier, user.Id.ToString()),
                        new(ClaimTypes.Name, user.Name),
                        new(ClaimTypes.Role, user.Role.ToString()),
                        new("IsDarkMode", user.IsDarkMode.ToString())
                    };
                    var identity = new ClaimsIdentity(claims, "MetroManiaAuth");
                    _currentUser = new ClaimsPrincipal(identity);
                }
            }
        }
        catch (InvalidOperationException)
        {
            // ProtectedSessionStorage is not available during prerendering
        }

        return new AuthenticationState(_currentUser);
    }

    public async Task LoginAsync(Guid userId)
    {
        await sessionStorage.SetAsync("userId", userId.ToString());
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }

    public async Task LogoutAsync()
    {
        await sessionStorage.DeleteAsync("userId");
        _currentUser = new ClaimsPrincipal(new ClaimsIdentity());
        NotifyAuthenticationStateChanged(
            Task.FromResult(new AuthenticationState(_currentUser)));
    }
}
