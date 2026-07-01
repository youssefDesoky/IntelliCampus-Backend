using IntelliCampus.Domain.Entities;
using IntelliCampus.Domain.Entities.Enums;
using IntelliCampus.Domain.Interfaces;
using IntelliCampus.Service.Exceptions;
using IntelliCampus.Service.Specifications;
using IntelliCampus.Service_Abstraction;
using IntelliCampus.shared.Pagination;
using IntelliCampus.Shared.Dtos.Inbox;
using IntelliCampus.Shared.Params;


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

        if (recipient.UserId == senderId)
            throw new InvalidOperationException("You cannot send a message to yourself.");

        var message = new InternalMessage
        {
            SenderId = senderId,
            RecipientId = recipient.UserId,
            ParentMessageId = parentMessageId,
            Subject = subject,
            Body = body,
            SentAt = EgyptTime.Now
        };

        var repo = _unitOfWork.GetRepository<InternalMessage, int>();
        repo.Add(message);
        await _unitOfWork.SaveChangesAsync();

        var sender = await _unitOfWork.GetRepository<User, int>().GetByIdAsync(senderId);
        var userMap = new Dictionary<int, User>
        {
            { senderId, sender },
            { recipient.UserId, recipient }
        };
        var dto = MapToDto(message, userMap);

        await _inboxHub.NotifyNewMessage(recipient.UserId, dto);

        try
        {
            var threadId = parentMessageId ?? message.MessageId;
            await _notificationService.SendAsync(
                recipient.UserId,
                NotificationType.NewMessage,
                $"New message: {subject}",
                title: sender?.FullName ?? "Unknown",
                clickUrl: $"/inbox/{threadId}");
        }
        catch
        {
        }

        return dto;
    }

    public async Task<PaginatedResult<InternalMessageDto>> GetInboxMessagesAsync(int userId, MessageQueryParams queryParams)
    {
        var repo = _unitOfWork.GetRepository<InternalMessage, int>();

        // Get root messages where user is recipient (paginated)
        var roots = await repo.GetAllAsync(InternalMessageSpec.InboxRoots(userId, queryParams), asNoTracking: true);
        var rootIds = roots.Select(r => r.MessageId).ToList();

        // Get all replies to those roots that user can see
        var replies = rootIds.Any()
            ? await repo.GetAllAsync(InternalMessageSpec.RepliesToRoots(rootIds, userId), asNoTracking: true)
            : new List<InternalMessage>();

        var dataToReturn = (await BuildThreadsAsync(roots, replies)).ToList();

        var countSpec = InternalMessageCountSpec.InboxRoots(userId, queryParams);
        var totalCount = await repo.CountAsync(countSpec);

        return new PaginatedResult<InternalMessageDto>(queryParams.PageIndex, dataToReturn.Count, totalCount, dataToReturn);
    }

    public async Task<IEnumerable<InternalMessageDto>> GetSentMessagesAsync(int userId, MessageQueryParams queryParams)
    {
        var repo = _unitOfWork.GetRepository<InternalMessage, int>();

        // Get root messages where user is sender
        var roots = await repo.GetAllAsync(InternalMessageSpec.SentRoots(userId, queryParams), asNoTracking: true);
        var rootIds = roots.Select(r => r.MessageId).ToList();

        // Get all replies to those roots that user can see
        var replies = rootIds.Any()
            ? await repo.GetAllAsync(InternalMessageSpec.RepliesToRoots(rootIds, userId), asNoTracking: true)
            : new List<InternalMessage>();

        return await BuildThreadsAsync(roots, replies);
    }

    public async Task MarkAsReadAsync(int userId, int messageId)
    {
        var spec = InternalMessageSpec.ById(messageId);
        var message = await _unitOfWork.GetRepository<InternalMessage, int>().GetByIdAsync(spec)
            ?? throw new InternalMessageNotFoundException(messageId);

        var threadId = message.ParentMessageId ?? message.MessageId;
        var repo = _unitOfWork.GetRepository<InternalMessage, int>();

        var allMessages = new List<InternalMessage> { message };
        var replies = await repo.GetAllAsync(InternalMessageSpec.RepliesToRoots([threadId], userId), asNoTracking: false);
        allMessages.AddRange(replies);

        var changed = false;
        foreach (var msg in allMessages.Where(m => m.RecipientId == userId && !m.IsRead))
        {
            msg.IsRead = true;
            msg.ReadAt = EgyptTime.Now;
            changed = true;
        }

        if (changed)
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
            throw new ForbiddenException("User cannot delete this message");

        if (message.IsDeletedBySender && message.IsDeletedByRecipient)
            _unitOfWork.GetRepository<InternalMessage, int>().Delete(message);

        await _unitOfWork.SaveChangesAsync();
    }

    private async Task<IEnumerable<InternalMessageDto>> BuildThreadsAsync(
        IEnumerable<InternalMessage> roots, IEnumerable<InternalMessage> replies)
    {
        var rootList = roots.ToList();
        var replyList = replies.ToList();

        if (rootList.Count == 0 && replyList.Count == 0)
            return [];

        var allUserIds = rootList.Select(r => r.SenderId)
            .Concat(rootList.Select(r => r.RecipientId))
            .Concat(replyList.Select(r => r.SenderId))
            .Concat(replyList.Select(r => r.RecipientId))
            .Distinct()
            .ToList();

        var userMap = allUserIds.Count > 0
            ? (await _unitOfWork.GetRepository<User, int>().GetAllAsync(new UsersByIdsSpec(allUserIds), asNoTracking: true))
                .ToDictionary(u => u.UserId)
            : new Dictionary<int, User>();

        var rootDtos = new List<InternalMessageDto>();
        foreach (var root in rootList)
        {
            var rootDto = MapToDto(root, userMap);
            foreach (var reply in replyList.Where(r => r.ParentMessageId == root.MessageId))
            {
                rootDto.Replies.Add(MapToDto(reply, userMap));
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

    private static InternalMessageDto MapToDto(InternalMessage m, Dictionary<int, User> userMap)
    {
        var sender = userMap.GetValueOrDefault(m.SenderId);
        var recipient = userMap.GetValueOrDefault(m.RecipientId);

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


