using System.Security.Claims;
using MetroMania.Domain.Enums;
using Microsoft.AspNetCore.Components.Authorization;

namespace MetroMania.Web.Services;

public class CookieAuthStateProvider : AuthenticationStateProvider
{
    private readonly ClaimsPrincipal? _initialHttpUser;
    private readonly MetroManiaApiClient _apiClient;

    public CookieAuthStateProvider(IHttpContextAccessor httpContextAccessor, MetroManiaApiClient apiClient)
    {
        _apiClient = apiClient;
        _initialHttpUser = httpContextAccessor.HttpContext?.User;
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        if (_initialHttpUser?.Identity?.IsAuthenticated == true)
        {
            var userIdStr = _initialHttpUser.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (Guid.TryParse(userIdStr, out var userId))
            {
                var user = await _apiClient.GetUserByIdAsync(userId);

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
