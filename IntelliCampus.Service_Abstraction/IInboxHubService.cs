using IntelliCampus.Shared.Dtos.Inbox;

namespace IntelliCampus.Service_Abstraction;

public interface IInboxHubService
{
    Task NotifyNewMessage(int recipientId, InternalMessageDto dto);
}
