using IntelliCampus.Domain.Entities;
using IntelliCampus.Domain.Interfaces;
using IntelliCampus.Service_Abstraction;
using IntelliCampus.Service.Exceptions;
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

    private IGenericRepository<User, int> Users
        => _unitOfWork.GetRepository<User, int>();

    public async Task<ChatMessageDto> SendMessageAsync(string senderId, string recipientId, string content, string? groupName = null)
    {
        var message = new ChatMessage
        {
            SenderId = senderId,
            RecipientId = recipientId,
            Content = content,
            Timestamp = EgyptTime.Now,
            GroupName = groupName
        };

        Messages.Add(message);
        await _unitOfWork.SaveChangesAsync();

        return await MapToDtoAsync(message);
    }

    public async Task<IEnumerable<ChatMessageDto>> GetChatHistoryAsync(string userId1, string userId2)
    {
        if (!int.TryParse(userId1, out var id1) || !int.TryParse(userId2, out var id2))
            throw new InvalidOperationException("Invalid user IDs.");

        var user1 = await Users.GetByIdAsync(id1);
        if (user1 is null)
            throw new UserNotFoundException(id1);

        var user2 = await Users.GetByIdAsync(id2);
        if (user2 is null)
            throw new UserNotFoundException(id2);

        var messages = (await Messages.GetAllAsync())
            .Where(m =>
                (m.SenderId == userId1 && m.RecipientId == userId2) ||
                (m.SenderId == userId2 && m.RecipientId == userId1)
            )
            .OrderByDescending(m => m.Timestamp)
            .ToList();

        return await MapToDtoListAsync(messages);
    }

    public async Task<IEnumerable<ChatMessageDto>> GetGroupChatHistoryAsync(string groupName)
    {
        var messages = (await Messages.GetAllAsync())
            .Where(m => m.GroupName == groupName)
            .OrderByDescending(m => m.Timestamp)
            .ToList();

        return await MapToDtoListAsync(messages);
    }

    private async Task<ChatMessageDto> MapToDtoAsync(ChatMessage m)
    {
        var sender = await Users.GetByIdAsync(int.Parse(m.SenderId));
        User? recipient = null;
        if (!string.IsNullOrEmpty(m.RecipientId))
            recipient = await Users.GetByIdAsync(int.Parse(m.RecipientId));

        return new ChatMessageDto
        {
            MessageId = m.MessageId,
            Content = m.Content,
            Timestamp = m.Timestamp,
            SenderId = m.SenderId,
            SenderName = sender?.FullName,
            RecipientId = m.RecipientId,
            RecipientName = recipient?.FullName,
            GroupName = m.GroupName,
            IsEdited = m.IsEdited,
            IsPinned = m.IsPinned
        };
    }

    private async Task<IEnumerable<ChatMessageDto>> MapToDtoListAsync(IEnumerable<ChatMessage> messages)
    {
        var results = new List<ChatMessageDto>();
        foreach (var m in messages)
            results.Add(await MapToDtoAsync(m));
        return results;
    }

    public Task MarkMessageAsReadAsync(string userId, string messageId)
    {
        // Not supported: ChatMessage does not have IsRead property
        throw new NotSupportedException("Mark as read is not supported. Add IsRead property to ChatMessage if needed.");
    }

    public async Task<ChatMessageDto?> DeleteMessageAsync(string userId, string messageId)
    {
        if (!int.TryParse(messageId, out var msgId))
            throw new ArgumentException("Invalid messageId");

        var message = await Messages.GetByIdAsync(msgId);
        if (message == null)
            throw new ChatMessageNotFoundException("Message not found");

            // Only sender or recipient can delete
        if (message.SenderId != userId && message.RecipientId != userId)
            throw new UnauthorizedAccessException("User cannot delete this message");

        var dto = await MapToDtoAsync(message);
        Messages.Delete(message);
        await _unitOfWork.SaveChangesAsync();
        return dto;
    }

    public async Task<ChatMessageDto?> PinMessageAsync(string userId, string messageId)
    {
        if (!int.TryParse(messageId, out var msgId))
            throw new ArgumentException("Invalid messageId");

        var message = await Messages.GetByIdAsync(msgId);
        if (message == null)
            throw new ChatMessageNotFoundException("Message not found");

        if (message.SenderId != userId && message.RecipientId != userId)
            throw new UnauthorizedAccessException("User cannot pin this message");

        // Unpin any previously pinned message in the same chat
        var allMessages = await Messages.GetAllAsync();
        ChatMessage? oldPinned;
        if (!string.IsNullOrEmpty(message.GroupName))
        {
            oldPinned = allMessages.FirstOrDefault(m =>
                m.GroupName == message.GroupName && m.IsPinned && m.MessageId != msgId);
        }
        else
        {
            oldPinned = allMessages.FirstOrDefault(m =>
                ((m.SenderId == message.SenderId && m.RecipientId == message.RecipientId) ||
                 (m.SenderId == message.RecipientId && m.RecipientId == message.SenderId))
                && m.IsPinned && m.MessageId != msgId);
        }

        if (oldPinned != null)
        {
            oldPinned.IsPinned = false;
            Messages.Update(oldPinned);
        }

        message.IsPinned = true;
        Messages.Update(message);
        await _unitOfWork.SaveChangesAsync();
        return await MapToDtoAsync(message);
    }

    public async Task<ChatMessageDto?> UnpinMessageAsync(string userId, string messageId)
    {
        if (!int.TryParse(messageId, out var msgId))
            throw new ArgumentException("Invalid messageId");

        var message = await Messages.GetByIdAsync(msgId);
        if (message == null)
            throw new ChatMessageNotFoundException("Message not found");

        if (message.SenderId != userId && message.RecipientId != userId)
            throw new UnauthorizedAccessException("User cannot unpin this message");

        message.IsPinned = false;
        Messages.Update(message);
        await _unitOfWork.SaveChangesAsync();
        return await MapToDtoAsync(message);
    }

    public async Task<ChatMessageDto?> EditMessageAsync(string userId, string messageId, string newContent)
    {
        if (!int.TryParse(messageId, out var msgId))
            throw new ArgumentException("Invalid messageId");

        var message = await Messages.GetByIdAsync(msgId);
        if (message == null)
            throw new ChatMessageNotFoundException("Message not found");

        // Only sender can edit
        if (message.SenderId != userId)
            throw new UnauthorizedAccessException("User cannot edit this message");

        message.Content = newContent;
        message.IsEdited = true;
        Messages.Update(message);
        await _unitOfWork.SaveChangesAsync();
        return await MapToDtoAsync(message);
    }
}
