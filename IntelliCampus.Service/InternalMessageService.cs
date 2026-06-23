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

    public async Task<InternalMessageDto> SendMessageAsync(int senderId, string recipientEmail, string subject, string body)
    {
        var recipient = await _unitOfWork.GetRepository<User, int>().GetByIdAsync(
            new UserByEmailSpec(recipientEmail))
            ?? throw new UserNotFoundException($"User with email '{recipientEmail}' not found");

        var message = new InternalMessage
        {
            SenderId = senderId,
            RecipientId = recipient.UserId,
            Subject = subject,
            Body = body,
            SentAt = EgyptTime.Now
        };

        var repo = _unitOfWork.GetRepository<InternalMessage, int>();
        repo.Add(message);
        await _unitOfWork.SaveChangesAsync();

        var dto = await MapToDtoAsync(message);

        try
        {
            var sender = await _unitOfWork.GetRepository<User, int>().GetByIdAsync(senderId);
            await _notificationService.SendAsync(
                recipient.UserId,
                NotificationType.NewMessage,
                $"New message: {subject}",
                title: sender?.FullName ?? "Unknown",
                clickUrl: "/messages/inbox");
            await _inboxHub.NotifyNewMessage(recipient.UserId, dto);
        }
        catch
        {
        }

        return dto;
    }

    public async Task<IEnumerable<InternalMessageDto>> GetInboxMessagesAsync(int userId)
    {
        var spec = InternalMessageSpec.Inbox(userId, includeRead: true);
        var messages = await _unitOfWork.GetRepository<InternalMessage, int>().GetAllAsync(spec);
        return await MapToDtoListAsync(messages);
    }

    public async Task<IEnumerable<InternalMessageDto>> GetSentMessagesAsync(int userId)
    {
        var spec = InternalMessageSpec.Sent(userId);
        var messages = await _unitOfWork.GetRepository<InternalMessage, int>().GetAllAsync(spec);
        return await MapToDtoListAsync(messages);
    }

    public async Task MarkAsReadAsync(int userId, int messageId)
    {
        var spec = InternalMessageSpec.ById(messageId);
        var message = await _unitOfWork.GetRepository<InternalMessage, int>().GetByIdAsync(spec)
            ?? throw new InternalMessageNotFoundException(messageId);

        if (message.RecipientId != userId)
            throw new UnauthorizedAccessException("Only the recipient can mark a message as read");

        message.IsRead = true;
        message.ReadAt = EgyptTime.Now;
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

    private async Task<IEnumerable<InternalMessageDto>> MapToDtoListAsync(IEnumerable<InternalMessage> messages)
    {
        var results = new List<InternalMessageDto>();
        foreach (var m in messages)
            results.Add(await MapToDtoAsync(m));
        return results;
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
            RecipientId = m.RecipientId,
            RecipientName = recipient?.FullName ?? "Unknown",
            SentAt = m.SentAt.ToString("dd MM yyyy hh:mm:ss"),
            IsRead = m.IsRead,
            ReadAt = m.ReadAt?.ToString("dd MM yyyy hh:mm:ss")
        };
    }
}


