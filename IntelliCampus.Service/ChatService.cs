using IntelliCampus.Domain.Entities;
using IntelliCampus.Domain.Interfaces;
using IntelliCampus.Service_Abstraction;
using IntelliCampus.Shared.Dtos.ChatMessage;

namespace IntelliCampus.Service;

public class ChatService : IChatService
{
    private readonly IUnitOfWork _unitOfWork;

    public ChatService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    private IGenericRepository<ChatMessage, int> Messages
        => _unitOfWork.GetRepository<ChatMessage, int>();

    public async Task<ChatMessageDto> SendMessageAsync(string senderId, string recipientId, string content)
    {
        var message = new ChatMessage
        {
            SenderId = senderId,
            RecipientId = recipientId,
            Content = content,
            Timestamp = DateTime.UtcNow
        };

        Messages.Add(message);
        await _unitOfWork.SaveChangesAsync();

        return MapToDto(message);
    }

    public async Task<IEnumerable<ChatMessageDto>> GetChatHistoryAsync(string userId1, string userId2)
    {
        var messages = (await Messages.GetAllAsync())
            .Where(m =>
                (m.SenderId == userId1 && m.RecipientId == userId2) ||
                (m.SenderId == userId2 && m.RecipientId == userId1)
            )
            .OrderByDescending(m => m.Timestamp)
            .ToList();

        return messages.Select(MapToDto);
    }

    public async Task<IEnumerable<ChatMessageDto>> GetGroupChatHistoryAsync(string groupName)
    {
        var messages = (await Messages.GetAllAsync())
            .Where(m => m.GroupName == groupName)
            .OrderByDescending(m => m.Timestamp)
            .ToList();

        return messages.Select(MapToDto);
    }

    private static ChatMessageDto MapToDto(ChatMessage m) => new()
    {
        MessageId = m.MessageId,
        Content = m.Content,
        Timestamp = m.Timestamp,
        SenderId = m.SenderId,
        SenderName = m.Sender?.FullName,
        RecipientId = m.RecipientId,
        RecipientName = m.Recipient?.FullName,
        GroupName = m.GroupName
    };

    public Task MarkMessageAsReadAsync(string userId, string messageId)
    {
        // Not supported: ChatMessage does not have IsRead property
        throw new NotSupportedException("Mark as read is not supported. Add IsRead property to ChatMessage if needed.");
    }

    public async Task DeleteMessageAsync(string userId, string messageId)
    {
        if (!int.TryParse(messageId, out var msgId))
            throw new ArgumentException("Invalid messageId");

        var message = await Messages.GetByIdAsync(msgId);
        if (message == null)
            throw new InvalidOperationException("Message not found");

        // Only sender or recipient can delete
        if (message.SenderId != userId && message.RecipientId != userId)
            throw new UnauthorizedAccessException("User cannot delete this message");

        Messages.Delete(message);
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task EditMessageAsync(string userId, string messageId, string newContent)
    {
        if (!int.TryParse(messageId, out var msgId))
            throw new ArgumentException("Invalid messageId");

        var message = await Messages.GetByIdAsync(msgId);
        if (message == null)
            throw new InvalidOperationException("Message not found");

        // Only sender can edit
        if (message.SenderId != userId)
            throw new UnauthorizedAccessException("User cannot edit this message");

        message.Content = newContent;
        Messages.Update(message);
        await _unitOfWork.SaveChangesAsync();
    }
}
