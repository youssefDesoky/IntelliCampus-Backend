using IntelliCampus.shared.Pagination;
using IntelliCampus.Shared.Dtos.Inbox;
using IntelliCampus.Shared.Params;

namespace IntelliCampus.Service_Abstraction;

public interface IInternalMessageService
{
    Task<InternalMessageDto> SendMessageAsync(int senderId, string recipientEmail, string subject, string body, int? parentMessageId = null);
    Task<PaginatedResult<InternalMessageDto>> GetInboxMessagesAsync(int userId, MessageQueryParams queryParams);
    Task<IEnumerable<InternalMessageDto>> GetSentMessagesAsync(int userId, MessageQueryParams queryParams);
    Task MarkAsReadAsync(int userId, int messageId);
    Task DeleteMessageAsync(int userId, int messageId);
}
