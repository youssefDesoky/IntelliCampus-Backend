using IntelliCampus.Shared.Dtos.ChatMessage;

namespace IntelliCampus.Service_Abstraction;

public interface IChatService
{
    Task<ChatMessageDto> SendMessageAsync(string senderId, string recipientId, string content, string? groupName = null);
    Task<IEnumerable<ChatMessageDto>> GetChatHistoryAsync(string userId1, string userId2);
    Task<IEnumerable<ChatMessageDto>> GetGroupChatHistoryAsync(string groupName);
    Task MarkMessageAsReadAsync(string userId, string messageId);
    Task<ChatMessageDto?> DeleteMessageAsync(string userId, string messageId);
    Task<ChatMessageDto?> EditMessageAsync(string userId, string messageId, string newContent);
    Task<ChatMessageDto?> PinMessageAsync(string userId, string messageId);
    Task<ChatMessageDto?> UnpinMessageAsync(string userId, string messageId);
}