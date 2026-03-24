using System.Collections.Concurrent;

namespace MetroMania.Web.Services;

/// <summary>
/// Short-lived, single-use tickets that bridge the Blazor circuit login
/// to an HTTP cookie sign-in endpoint.
/// </summary>
public class LoginTicketService
{
    private readonly ConcurrentDictionary<string, (Guid UserId, DateTime Expiry)> _tickets = new();

    public string CreateTicket(Guid userId)
    {
        Cleanup();
        var ticket = Guid.NewGuid().ToString("N");
        _tickets[ticket] = (userId, DateTime.UtcNow.AddMinutes(1));
        return ticket;
    }

    public Guid? RedeemTicket(string ticket)
    {
        if (_tickets.TryRemove(ticket, out var data) && data.Expiry > DateTime.UtcNow)
            return data.UserId;
        return null;
    }

    private void Cleanup()
    {
        var expired = _tickets.Where(kvp => kvp.Value.Expiry < DateTime.UtcNow).Select(kvp => kvp.Key).ToList();
        foreach (var key in expired)
            _tickets.TryRemove(key, out _);
    }
}
