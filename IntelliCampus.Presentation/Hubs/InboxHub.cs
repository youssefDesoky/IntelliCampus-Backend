using Microsoft.AspNetCore.SignalR;

namespace IntelliCampus.Presentation.Hubs;

public class InboxHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        var userId = Context.UserIdentifier;
        await Groups.AddToGroupAsync(Context.ConnectionId, $"inbox_{userId}");
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = Context.UserIdentifier;
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"inbox_{userId}");
        await base.OnDisconnectedAsync(exception);
    }
}
