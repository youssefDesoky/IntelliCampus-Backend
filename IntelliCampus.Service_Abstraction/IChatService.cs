using IntelliCampus.Shared.Dtos.ChatMessage;

namespace IntelliCampus.Service_Abstraction;

public interface IChatService
{
    Task<ChatMessageDto> SendMessageAsync(string senderId, string recipientId, string content);
    Task<IEnumerable<ChatMessageDto>> GetChatHistoryAsync(string userId1, string userId2);
    Task<IEnumerable<ChatMessageDto>> GetGroupChatHistoryAsync(string groupName);
    Task MarkMessageAsReadAsync(string userId, string messageId);
    Task DeleteMessageAsync(string userId, string messageId);
    Task EditMessageAsync(string userId, string messageId, string newContent);
}