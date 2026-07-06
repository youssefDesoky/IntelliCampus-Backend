using IntelliCampus.Domain.Entities;
using IntelliCampus.Domain.Entities.Enums;
using IntelliCampus.Domain.Interfaces;
using IntelliCampus.Service_Abstraction;
using IntelliCampus.Service.Exceptions;
using IntelliCampus.Service.Specifications;
using IntelliCampus.Shared.Dtos.ChatMessage;
using Microsoft.Extensions.Logging;

namespace IntelliCampus.Service;

public class ChatService : IChatService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly INotificationService _notificationService;
    private readonly IFaheemAiService _faheemAi;
    private readonly ILogger<ChatService> _logger;

    private const string FahimSenderId = "-1";

    public ChatService(IUnitOfWork unitOfWork, INotificationService notificationService,
        IFaheemAiService faheemAi, ILogger<ChatService> logger)
    {
        _unitOfWork = unitOfWork;
        _notificationService = notificationService;
        _faheemAi = faheemAi;
        _logger = logger;
    }

    private IGenericRepository<ChatMessage, int> Messages
        => _unitOfWork.GetRepository<ChatMessage, int>();

    private IGenericRepository<User, int> Users
        => _unitOfWork.GetRepository<User, int>();

    private IGenericRepository<Student, int> Students
        => _unitOfWork.GetRepository<Student, int>();

    private IGenericRepository<ChatbotQuery, int> ChatbotQueries
        => _unitOfWork.GetRepository<ChatbotQuery, int>();

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

        var dto = await MapToDtoAsync(message);
        if (string.IsNullOrEmpty(groupName) && recipientId != FahimSenderId && int.TryParse(recipientId, out var recipientUserId))
        {
            var encodedName = Uri.EscapeDataString(dto.SenderName ?? "Unknown");
            await _notificationService.SendAsync(recipientUserId, NotificationType.NewMessage, dto.Content, dto.SenderName ?? "New Message", $"/?openChat=message&userId={senderId}&userName={encodedName}");
        }
        return dto;
    }

    public async Task<IEnumerable<ChatMessageDto>> GetChatHistoryAsync(string userId1, string userId2, int pageNumber = 1, int pageSize = 50)
    {
        foreach (var id in new[] { userId1, userId2 })
        {
            if (id == FahimSenderId) continue;
            if (!int.TryParse(id, out var uid))
                throw new InvalidOperationException("Invalid user ID.");
            var user = await Users.GetByIdAsync(uid);
            if (user is null)
                throw new UserNotFoundException(uid);
        }

        var messages = await Messages.GetAllAsync(
            new ChatMessageSpec(userId1, userId2, pageNumber, pageSize), asNoTracking: true);

        return await MapToDtoListAsync(messages);
    }

    public async Task<IEnumerable<ChatMessageDto>> GetGroupChatHistoryAsync(string groupName, int pageNumber = 1, int pageSize = 50)
    {
        var messages = await Messages.GetAllAsync(
            new ChatMessageSpec(groupName, pageNumber, pageSize), asNoTracking: true);

        return await MapToDtoListAsync(messages);
    }

    private async Task<ChatMessageDto> MapToDtoAsync(ChatMessage m)
    {
        var sender = await ResolveUserAsync(m.SenderId);
        User? recipient = null;
        if (!string.IsNullOrEmpty(m.RecipientId))
            recipient = await ResolveUserAsync(m.RecipientId);

        return new ChatMessageDto
        {
            MessageId = m.MessageId,
            Content = m.Content,
            Timestamp = m.Timestamp,
            SenderId = m.SenderId,
            SenderName = sender?.FullName ?? (m.SenderId == FahimSenderId ? "Fahim" : null),
            RecipientId = m.RecipientId,
            RecipientName = recipient?.FullName ?? (m.RecipientId == FahimSenderId ? "Fahim" : null),
            GroupName = m.GroupName,
            IsEdited = m.IsEdited,
            IsPinned = m.IsPinned
        };
    }

    private async Task<User?> ResolveUserAsync(string id)
    {
        if (id == FahimSenderId) return null;
        if (!int.TryParse(id, out var uid)) return null;
        return await Users.GetByIdAsync(uid);
    }

    private async Task<IEnumerable<ChatMessageDto>> MapToDtoListAsync(IEnumerable<ChatMessage> messages)
    {
        var messageList = messages.ToList();
        if (messageList.Count == 0)
            return [];

        // Batch load all referenced users (skip the bot sentinel)
        var userIds = new HashSet<int>();
        foreach (var m in messageList)
        {
            if (m.SenderId != FahimSenderId && int.TryParse(m.SenderId, out var sid)) userIds.Add(sid);
            if (m.RecipientId != FahimSenderId && int.TryParse(m.RecipientId, out var rid)) userIds.Add(rid);
        }

        var users = await Users.GetAllAsync(new UserSpec(userIds.ToList()), asNoTracking: true);
        var userMap = users.ToDictionary(u => u.UserId.ToString(), u => u.FullName);
        // Synthetic entry for the bot
        userMap[FahimSenderId] = "Fahim";

        return messageList.Select(m => new ChatMessageDto
        {
            MessageId = m.MessageId,
            Content = m.Content,
            Timestamp = m.Timestamp,
            SenderId = m.SenderId,
            SenderName = userMap.GetValueOrDefault(m.SenderId),
            RecipientId = m.RecipientId,
            RecipientName = userMap.GetValueOrDefault(m.RecipientId),
            GroupName = m.GroupName,
            IsEdited = m.IsEdited,
            IsPinned = m.IsPinned
        }).ToList();
    }

    public Task MarkMessageAsReadAsync(string userId, string messageId)
    {
        throw new NotSupportedException("Mark as read is not supported. Add IsRead property to ChatMessage if needed.");
    }

    public async Task<ChatMessageDto?> DeleteMessageAsync(string userId, string messageId)
    {
        if (!int.TryParse(messageId, out var msgId))
            throw new ArgumentException("Invalid messageId");

        var message = await Messages.GetByIdAsync(msgId);
        if (message == null)
            throw new ChatMessageNotFoundException("Message not found");

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

        ChatMessage? oldPinned;
        if (!string.IsNullOrEmpty(message.GroupName))
        {
            oldPinned = (await Messages.GetAllAsync(new ChatMessageSpec(message.GroupName, true)))
                .FirstOrDefault();
        }
        else
        {
            oldPinned = (await Messages.GetAllAsync(new ChatMessageSpec(message.SenderId, message.RecipientId, true)))
                .FirstOrDefault();
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

        if (message.SenderId != userId)
            throw new UnauthorizedAccessException("User cannot edit this message");

        message.Content = newContent;
        message.IsEdited = true;
        Messages.Update(message);
        await _unitOfWork.SaveChangesAsync();
        return await MapToDtoAsync(message);
    }

    public async Task<ChatMessageDto> GenerateFahimReplyAsync(string senderId, string question, CancellationToken ct = default)
    {
        // Resolve student info for advisor context
        var student = await Students.GetByIdAsync(int.Parse(senderId));
        var studentCode = student?.StudentCode;
        var department = student?.Department?.DepartmentName;

        string answer;
        try
        {
            answer = await _faheemAi.AskAdvisorAsync(question, studentCode, department, ct);
        }
        catch (FaheemAiException ex)
        {
            _logger.LogError(ex, "Faheem AI advisor call failed for user {SenderId}", senderId);
            return await GenerateFahimFallbackReplyAsync(senderId, ex, ct);
        }

        // Persist the question/answer pair
        if (student is not null)
        {
            ChatbotQueries.Add(new ChatbotQuery
            {
                Question = question,
                Response = answer,
                StudentId = student.UserId,
                Timestamp = EgyptTime.Now,
            });
        }

        // Persist Fahim's reply as a ChatMessage
        var replyMessage = new ChatMessage
        {
            SenderId = FahimSenderId,
            RecipientId = senderId,
            Content = answer,
            Timestamp = EgyptTime.Now,
        };
        Messages.Add(replyMessage);
        await _unitOfWork.SaveChangesAsync();

        return await MapToDtoAsync(replyMessage);
    }

    public async Task<ChatMessageDto> GenerateFahimCourseReplyAsync(string senderId, string courseCode, string courseName, string question, Stream? attachmentStream = null, string? attachmentFileName = null, CancellationToken ct = default)
    {
        var student = await Students.GetByIdAsync(int.Parse(senderId));
        var studentCode = student?.StudentCode;

        string answer;
        try
        {
            answer = await _faheemAi.AskCourseAsync(courseCode, question, studentCode, attachmentStream, attachmentFileName, ct);
        }
        catch (FaheemAiException ex) when (ex.Signal == "course_answer_error")
        {
            _logger.LogWarning("Course {Course} ({CourseName}) has no indexed materials for user {SenderId}", courseCode, courseName, senderId);
            answer = $"I don't have any course materials for **{courseName}** yet. I can only answer questions based on uploaded and indexed materials. Please ask your instructor to upload course materials, then I'll be able to help you with this course.";
        }
        catch (FaheemAiException ex)
        {
            _logger.LogError(ex, "Faheem AI course question failed for user {SenderId} course {Course}", senderId, courseCode);
            return await GenerateFahimFallbackReplyAsync(senderId, ex, ct);
        }

        if (student is not null)
        {
            ChatbotQueries.Add(new ChatbotQuery
            {
                Question = question,
                Response = answer,
                StudentId = student.UserId,
                Timestamp = EgyptTime.Now,
            });
        }

        var replyMessage = new ChatMessage
        {
            SenderId = FahimSenderId,
            RecipientId = senderId,
            Content = answer,
            Timestamp = EgyptTime.Now,
        };
        Messages.Add(replyMessage);
        await _unitOfWork.SaveChangesAsync();

        return await MapToDtoAsync(replyMessage);
    }

    public async Task<ChatMessageDto> GenerateFahimFallbackReplyAsync(string senderId, Exception? error = null, CancellationToken ct = default)
    {
        var replyMessage = new ChatMessage
        {
            SenderId = FahimSenderId,
            RecipientId = senderId,
            Content = "I'm sorry, I encountered an error processing your request. Please try again later.",
            Timestamp = EgyptTime.Now,
        };
        Messages.Add(replyMessage);
        await _unitOfWork.SaveChangesAsync();

        return await MapToDtoAsync(replyMessage);
    }
}
