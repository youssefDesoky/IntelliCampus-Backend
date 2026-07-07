using IntelliCampus.Shared.Dtos.ChatMessage;

namespace IntelliCampus.Service_Abstraction;

public interface IChatService
{
    Task<ChatMessageDto> SendMessageAsync(string senderId, string recipientId, string content, string? groupName = null);
    Task<IEnumerable<ChatMessageDto>> GetChatHistoryAsync(string userId1, string userId2, int pageNumber = 1, int pageSize = 50);
    Task<IEnumerable<ChatMessageDto>> GetGroupChatHistoryAsync(string groupName, int pageNumber = 1, int pageSize = 50);
    Task MarkMessageAsReadAsync(string userId, string messageId);
    Task<ChatMessageDto?> DeleteMessageAsync(string userId, string messageId);
    Task<ChatMessageDto?> EditMessageAsync(string userId, string messageId, string newContent);
    Task<ChatMessageDto?> PinMessageAsync(string userId, string messageId);
    Task<ChatMessageDto?> UnpinMessageAsync(string userId, string messageId);
    Task<ChatMessageDto> GenerateFahimReplyAsync(string senderId, string question, CancellationToken ct = default);
    Task<ChatMessageDto> GenerateFahimCourseReplyAsync(string senderId, string courseCode, string courseName, string question, System.IO.Stream? attachmentStream = null, string? attachmentFileName = null, CancellationToken ct = default);
    Task<ChatMessageDto> GenerateFahimFallbackReplyAsync(string senderId, Exception? error = null, CancellationToken ct = default);
}