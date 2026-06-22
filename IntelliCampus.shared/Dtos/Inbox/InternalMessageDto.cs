namespace IntelliCampus.Shared.Dtos.Inbox;

public class InternalMessageDto
{
    public int MessageId { get; set; }
    public string Subject { get; set; } = null!;
    public string Body { get; set; } = null!;
    public int SenderId { get; set; }
    public string SenderName { get; set; } = null!;
    public int RecipientId { get; set; }
    public string RecipientName { get; set; } = null!;
    public string SentAt { get; set; } = null!;
    public bool IsRead { get; set; }
    public string? ReadAt { get; set; }
}
