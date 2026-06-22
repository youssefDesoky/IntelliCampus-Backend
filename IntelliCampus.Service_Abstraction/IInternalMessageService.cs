using IntelliCampus.Shared.Dtos.Inbox;

namespace IntelliCampus.Service_Abstraction;

public interface IInternalMessageService
{
    Task<InternalMessageDto> SendMessageAsync(int senderId, int recipientId, string subject, string body);
    Task<IEnumerable<InternalMessageDto>> GetInboxMessagesAsync(int userId);
    Task<IEnumerable<InternalMessageDto>> GetSentMessagesAsync(int userId);
    Task MarkAsReadAsync(int userId, int messageId);
    Task DeleteMessageAsync(int userId, int messageId);
}
