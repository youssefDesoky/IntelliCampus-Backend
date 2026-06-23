namespace IntelliCampus.Shared.Dtos.Inbox;

public class SendMessageDto
{
    public string RecipientEmail { get; set; } = null!;
    public string Subject { get; set; } = null!;
    public string Body { get; set; } = null!;
    public int? ParentMessageId { get; set; }
}
