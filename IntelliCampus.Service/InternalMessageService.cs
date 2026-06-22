using IntelliCampus.Domain.Entities;
using IntelliCampus.Domain.Interfaces;
using IntelliCampus.Service.Exceptions;
using IntelliCampus.Service.Specifications;
using IntelliCampus.Service_Abstraction;
using IntelliCampus.Shared.Dtos.Inbox;

namespace IntelliCampus.Service;

public class InternalMessageService : IInternalMessageService
{
    private readonly IUnitOfWork _unitOfWork;

    public InternalMessageService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task SendMessageAsync(int senderId, int recipientId, string subject, string body)
    {
        var message = new InternalMessage
        {
            SenderId = senderId,
            RecipientId = recipientId,
            Subject = subject,
            Body = body,
            SentAt = DateTime.UtcNow
        };

        var repo = _unitOfWork.GetRepository<InternalMessage, int>();
        repo.Add(message);
        await _unitOfWork.SaveChangesAsync();
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
            SentAt = m.SentAt,
            IsRead = m.IsRead,
            ReadAt = m.ReadAt
        };
    }
}


