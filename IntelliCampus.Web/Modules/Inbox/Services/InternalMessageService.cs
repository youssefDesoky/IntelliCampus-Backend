using IntelliCampus.Presistence.Data.Contexts;
using IntelliCampus.Service.Exceptions;
using IntelliCampus.Web.Modules.Inbox.Data;
using IntelliCampus.Web.Modules.Inbox.Dtos;
using IntelliCampus.Web.Modules.Inbox.Models;
using IntelliCampus.Web.Modules.Inbox.Specifications;
using Microsoft.EntityFrameworkCore;

namespace IntelliCampus.Web.Modules.Inbox.Services;

public class InternalMessageService : IInternalMessageService
{
    private readonly InboxDbContext _db;
    private readonly IntelliCampusDbContext _mainDb;

    public InternalMessageService(InboxDbContext db, IntelliCampusDbContext mainDb)
    {
        _db = db;
        _mainDb = mainDb;
    }

    public async Task<InternalMessageDto> SendMessageAsync(int senderId, int recipientId, string subject, string body)
    {
        var message = new InternalMessage
        {
            SenderId = senderId,
            RecipientId = recipientId,
            Subject = subject,
            Body = body,
            SentAt = DateTime.UtcNow
        };

        _db.InternalMessages.Add(message);
        await _db.SaveChangesAsync();

        return await MapToDtoAsync(message);
    }

    public async Task<IEnumerable<InternalMessageDto>> GetInboxMessagesAsync(int userId)
    {
        var spec = InternalMessageSpec.Inbox(userId, includeRead: true);
        var messages = await ApplySpec(spec).ToListAsync();
        return await MapToDtoListAsync(messages);
    }

    public async Task<IEnumerable<InternalMessageDto>> GetSentMessagesAsync(int userId)
    {
        var spec = InternalMessageSpec.Sent(userId);
        var messages = await ApplySpec(spec).ToListAsync();
        return await MapToDtoListAsync(messages);
    }

    public async Task MarkAsReadAsync(int userId, int messageId)
    {
        var spec = InternalMessageSpec.ById(messageId);
        var message = await ApplySpec(spec).FirstOrDefaultAsync()
            ?? throw new InternalMessageNotFoundException(messageId);

        if (message.RecipientId != userId)
            throw new UnauthorizedAccessException("Only the recipient can mark a message as read");

        message.IsRead = true;
        message.ReadAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
    }

    public async Task DeleteMessageAsync(int userId, int messageId)
    {
        var spec = InternalMessageSpec.ById(messageId);
        var message = await ApplySpec(spec).FirstOrDefaultAsync()
            ?? throw new InternalMessageNotFoundException(messageId);

        if (message.SenderId == userId)
            message.IsDeletedBySender = true;
        else if (message.RecipientId == userId)
            message.IsDeletedByRecipient = true;
        else
            throw new UnauthorizedAccessException("User cannot delete this message");

        if (message.IsDeletedBySender && message.IsDeletedByRecipient)
            _db.InternalMessages.Remove(message);

        await _db.SaveChangesAsync();
    }

    private IQueryable<InternalMessage> ApplySpec(InternalMessageSpec spec)
    {
        var query = _db.InternalMessages.AsQueryable();

        if (spec.Criteria is not null)
            query = query.Where(spec.Criteria);

        foreach (var include in spec.IncludeStrings)
            query = query.Include(include);

        foreach (var include in spec.IncludeExpressions)
            query = query.Include(include);

        if (spec.OrderByDescending is not null)
            query = query.OrderByDescending(spec.OrderByDescending);
        else if (spec.OrderBy is not null)
            query = query.OrderBy(spec.OrderBy);

        return query;
    }

    private async Task<InternalMessageDto> MapToDtoAsync(InternalMessage m)
    {
        var sender = await _mainDb.Users.FindAsync(m.SenderId);
        var recipient = await _mainDb.Users.FindAsync(m.RecipientId);

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

    private async Task<IEnumerable<InternalMessageDto>> MapToDtoListAsync(IEnumerable<InternalMessage> messages)
    {
        var results = new List<InternalMessageDto>();
        foreach (var m in messages)
            results.Add(await MapToDtoAsync(m));
        return results;
    }
}

public sealed class InternalMessageNotFoundException : NotFoundException
{
    public InternalMessageNotFoundException(int id) : base($"Internal message With Id {id} Is Not Found") { }
    public InternalMessageNotFoundException(string message) : base(message) { }
}
