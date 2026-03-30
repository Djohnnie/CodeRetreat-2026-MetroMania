using Microsoft.AspNetCore.SignalR;

namespace MetroMania.Api.Hubs;

public class SubmissionHub : Hub
{
    public async Task JoinUserGroup(string userId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, userId);
    }
}
