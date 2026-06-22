using IntelliCampus.Web.Modules.Inbox.Dtos;

namespace IntelliCampus.Web.Modules.Inbox.Services;

public interface IInternalMessageService
{
    Task<InternalMessageDto> SendMessageAsync(int senderId, int recipientId, string subject, string body);
    Task<IEnumerable<InternalMessageDto>> GetInboxMessagesAsync(int userId);
    Task<IEnumerable<InternalMessageDto>> GetSentMessagesAsync(int userId);
    Task MarkAsReadAsync(int userId, int messageId);
    Task DeleteMessageAsync(int userId, int messageId);
}
