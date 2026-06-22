namespace IntelliCampus.Shared.Dtos.Inbox;

public class SendMessageDto
{
    public int RecipientId { get; set; }
    public string Subject { get; set; } = null!;
    public string Body { get; set; } = null!;
}
