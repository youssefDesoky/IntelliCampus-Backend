using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace IntelliCampus.Presentation.Hubs;

[Authorize]
public class InboxHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        var userId = Context.UserIdentifier;
        if (string.IsNullOrEmpty(userId))
        {
            throw new HubException("User identifier is missing. Ensure the connection is authenticated.");
        }
        await Groups.AddToGroupAsync(Context.ConnectionId, $"inbox_{userId}");
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = Context.UserIdentifier;
        if (!string.IsNullOrEmpty(userId))
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"inbox_{userId}");
        }
        await base.OnDisconnectedAsync(exception);
    }
}
