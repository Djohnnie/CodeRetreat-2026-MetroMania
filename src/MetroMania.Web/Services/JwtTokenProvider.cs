using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace MetroMania.Web.Services;

/// <summary>
/// Scoped (circuit-lifetime) service that generates JWT tokens from the
/// authenticated user's cookie claims for API calls.
/// </summary>
public class JwtTokenProvider(IHttpContextAccessor httpContextAccessor, IConfiguration configuration)
{
    private string? _cachedToken;
    private DateTime _tokenExpiry;
    private ClaimsPrincipal? _cachedPrincipal;

    public string? GetToken()
    {
        // Return cached token if still valid (with 5-minute buffer)
        if (_cachedToken is not null && DateTime.UtcNow < _tokenExpiry.AddMinutes(-5))
            return _cachedToken;

        // Prefer HttpContext (available during initial HTTP request), fall back to cached principal (SignalR)
        var principal = httpContextAccessor.HttpContext?.User ?? _cachedPrincipal;

        if (principal?.Identity?.IsAuthenticated != true)
            return null;

        _cachedPrincipal ??= principal;

        var secretKey = configuration["Jwt:SecretKey"]
            ?? throw new InvalidOperationException("Jwt:SecretKey is not configured.");
        var issuer = configuration["Jwt:Issuer"];
        var audience = configuration["Jwt:Audience"];
        var expirationMinutes = int.Parse(configuration["Jwt:ExpirationMinutes"] ?? "60");

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: principal.Claims,
            expires: DateTime.UtcNow.AddMinutes(expirationMinutes),
            signingCredentials: credentials);

        _cachedToken = new JwtSecurityTokenHandler().WriteToken(token);
        _tokenExpiry = token.ValidTo;

        return _cachedToken;
    }
}
