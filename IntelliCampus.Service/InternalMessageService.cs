using IntelliCampus.Domain.Entities;
using IntelliCampus.Domain.Entities.Enums;
using IntelliCampus.Domain.Interfaces;
using IntelliCampus.Service.Exceptions;
using IntelliCampus.Service.Specifications;
using IntelliCampus.Service_Abstraction;
using IntelliCampus.Shared.Dtos.Inbox;


namespace IntelliCampus.Service;

public class InternalMessageService : IInternalMessageService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly INotificationService _notificationService;
    private readonly IInboxHubService _inboxHub;

    public InternalMessageService(IUnitOfWork unitOfWork, INotificationService notificationService, IInboxHubService inboxHub)
    {
        _unitOfWork = unitOfWork;
        _notificationService = notificationService;
        _inboxHub = inboxHub;
    }

    public async Task<InternalMessageDto> SendMessageAsync(int senderId, string recipientEmail, string subject, string body, int? parentMessageId = null)
    {
        var recipient = await _unitOfWork.GetRepository<User, int>().GetByIdAsync(
            new UserByEmailSpec(recipientEmail))
            ?? throw new UserNotFoundException($"User with email '{recipientEmail}' not found");

        var message = new InternalMessage
        {
            SenderId = senderId,
            RecipientId = recipient.UserId,
            ParentMessageId = parentMessageId,
            Subject = subject,
            Body = body,
            SentAt = DateTime.UtcNow
        };

        var repo = _unitOfWork.GetRepository<InternalMessage, int>();
        repo.Add(message);
        await _unitOfWork.SaveChangesAsync();

        var dto = await MapToDtoAsync(message);

        await _inboxHub.NotifyNewMessage(recipient.UserId, dto);

        try
        {
            var sender = await _unitOfWork.GetRepository<User, int>().GetByIdAsync(senderId);
            await _notificationService.SendAsync(
                recipient.UserId,
                NotificationType.NewMessage,
                $"New message: {subject}",
                title: sender?.FullName ?? "Unknown",
                clickUrl: "/messages/inbox");
        }
        catch
        {
        }

        return dto;
    }

    public async Task<IEnumerable<InternalMessageDto>> GetInboxMessagesAsync(int userId)
    {
        var repo = _unitOfWork.GetRepository<InternalMessage, int>();

        // Get root messages where user is recipient
        var roots = await repo.GetAllAsync(InternalMessageSpec.InboxRoots(userId));
        var rootIds = roots.Select(r => r.MessageId).ToList();

        // Get all replies to those roots that user can see
        var replies = rootIds.Any()
            ? await repo.GetAllAsync(InternalMessageSpec.RepliesToRoots(rootIds, userId))
            : new List<InternalMessage>();

        return await BuildThreadsAsync(roots, replies);
    }

    public async Task<IEnumerable<InternalMessageDto>> GetSentMessagesAsync(int userId)
    {
        var repo = _unitOfWork.GetRepository<InternalMessage, int>();

        // Get root messages where user is sender
        var roots = await repo.GetAllAsync(InternalMessageSpec.SentRoots(userId));
        var rootIds = roots.Select(r => r.MessageId).ToList();

        // Get all replies to those roots that user can see
        var replies = rootIds.Any()
            ? await repo.GetAllAsync(InternalMessageSpec.RepliesToRoots(rootIds, userId))
            : new List<InternalMessage>();

        return await BuildThreadsAsync(roots, replies);
    }

    public async Task MarkAsReadAsync(int userId, int messageId)
    {
        var spec = InternalMessageSpec.ById(messageId);
        var message = await _unitOfWork.GetRepository<InternalMessage, int>().GetByIdAsync(spec)
            ?? throw new InternalMessageNotFoundException(messageId);

        if (message.RecipientId != userId)
            throw new UnauthorizedAccessException("Only the recipient can mark a message as read");

        message.IsRead = true;
        message.ReadAt = DateTime.UtcNow;
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task DeleteMessageAsync(int userId, int messageId)
    {
        var spec = InternalMessageSpec.ById(messageId);
        var message = await _unitOfWork.GetRepository<InternalMessage, int>().GetByIdAsync(spec)
            ?? throw new InternalMessageNotFoundException(messageId);

        if (message.SenderId == userId)
            message.IsDeletedBySender = true;
        else if (message.RecipientId == userId)
            message.IsDeletedByRecipient = true;
        else
            throw new UnauthorizedAccessException("User cannot delete this message");

        if (message.IsDeletedBySender && message.IsDeletedByRecipient)
            _unitOfWork.GetRepository<InternalMessage, int>().Delete(message);

        await _unitOfWork.SaveChangesAsync();
    }

    private async Task<IEnumerable<InternalMessageDto>> BuildThreadsAsync(
        IEnumerable<InternalMessage> roots, IEnumerable<InternalMessage> replies)
    {
        var replyList = replies.ToList();
        var rootDtos = new List<InternalMessageDto>();

        foreach (var root in roots)
        {
            var rootDto = await MapToDtoAsync(root);
            foreach (var reply in replyList.Where(r => r.ParentMessageId == root.MessageId))
            {
                rootDto.Replies.Add(await MapToDtoAsync(reply));
            }
            rootDtos.Add(rootDto);
        }

        // Sort by latest activity in thread (root SentAt or latest reply SentAt)
        return rootDtos.OrderByDescending(d =>
        {
            var latest = d.Replies.Any()
                ? d.Replies.Max(r => ParseSentAt(r.SentAt))
                : ParseSentAt(d.SentAt);
            return latest;
        });
    }

    private static DateTime ParseSentAt(string sentAt)
    {
        if (DateTime.TryParseExact(sentAt, "dd MM yyyy hh:mm:ss",
            System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.None, out var dt))
            return dt;
        return DateTime.MinValue;
    }

    private async Task<InternalMessageDto> MapToDtoAsync(InternalMessage m)
    {
        var userRepo = _unitOfWork.GetRepository<User, int>();
        var sender = await userRepo.GetByIdAsync(m.SenderId);
        var recipient = await userRepo.GetByIdAsync(m.RecipientId);

        return new InternalMessageDto
        {
            MessageId = m.MessageId,
            Subject = m.Subject,
            Body = m.Body,
            SenderId = m.SenderId,
            SenderName = sender?.FullName ?? "Unknown",
            SenderEmail = sender?.Email ?? "",
            RecipientId = m.RecipientId,
            RecipientName = recipient?.FullName ?? "Unknown",
            RecipientEmail = recipient?.Email ?? "",
            ParentMessageId = m.ParentMessageId,
            SentAt = m.SentAt.ToString("dd MM yyyy hh:mm:ss"),
            IsRead = m.IsRead,
            ReadAt = m.ReadAt?.ToString("dd MM yyyy hh:mm:ss")
        };
    }
}


