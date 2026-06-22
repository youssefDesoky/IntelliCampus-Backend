using IntelliCampus.Shared.Dtos.Inbox;

namespace IntelliCampus.Service_Abstraction;

public interface IInternalMessageService
{
    Task<InternalMessageDto> SendMessageAsync(int senderId, string recipientEmail, string subject, string body, int? parentMessageId = null);
    Task<IEnumerable<InternalMessageDto>> GetInboxMessagesAsync(int userId);
    Task<IEnumerable<InternalMessageDto>> GetSentMessagesAsync(int userId);
    Task MarkAsReadAsync(int userId, int messageId);
    Task DeleteMessageAsync(int userId, int messageId);
}
