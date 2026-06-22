using IntelliCampus.Service_Abstraction;
using IntelliCampus.Shared.Dtos.Inbox;
using Microsoft.AspNetCore.SignalR;

namespace IntelliCampus.Presentation.Hubs;

public class InboxHubService : IInboxHubService
{
    private readonly IHubContext<InboxHub> _hubContext;

    public InboxHubService(IHubContext<InboxHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public async Task NotifyNewMessage(int recipientId, InternalMessageDto dto)
    {
        await _hubContext.Clients.Group($"inbox_{recipientId}").SendAsync("NewMessage", dto);
    }
}
