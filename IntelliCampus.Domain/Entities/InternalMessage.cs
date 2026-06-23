namespace IntelliCampus.Domain.Entities;

public class InternalMessage
{
    public int MessageId { get; set; }
    public string Subject { get; set; } = null!;
    public string Body { get; set; } = null!;
    public int SenderId { get; set; }
    public int RecipientId { get; set; }
    public int? ParentMessageId { get; set; }
    public DateTime SentAt { get; set; } = EgyptTime.Now;
    public bool IsRead { get; set; }
    public DateTime? ReadAt { get; set; }
    public bool IsDeletedBySender { get; set; }
    public bool IsDeletedByRecipient { get; set; }
}
