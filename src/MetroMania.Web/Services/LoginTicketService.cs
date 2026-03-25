using System.Collections.Concurrent;
using MetroMania.Application.DTOs;

namespace MetroMania.Web.Services;

/// <summary>
/// Short-lived, single-use tickets that bridge the Blazor circuit login
/// to an HTTP cookie sign-in endpoint. Stores the full UserDto so the
/// callback can create claims without calling the API again.
/// </summary>
public class LoginTicketService
{
    private readonly ConcurrentDictionary<string, (UserDto User, DateTime Expiry)> _tickets = new();

    public string CreateTicket(UserDto user)
    {
        Cleanup();
        var ticket = Guid.NewGuid().ToString("N");
        _tickets[ticket] = (user, DateTime.UtcNow.AddMinutes(1));
        return ticket;
    }

    public UserDto? RedeemTicket(string ticket)
    {
        if (_tickets.TryRemove(ticket, out var data) && data.Expiry > DateTime.UtcNow)
            return data.User;
        return null;
    }

    private void Cleanup()
    {
        var expired = _tickets.Where(kvp => kvp.Value.Expiry < DateTime.UtcNow).Select(kvp => kvp.Key).ToList();
        foreach (var key in expired)
            _tickets.TryRemove(key, out _);
    }
}
