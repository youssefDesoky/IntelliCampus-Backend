namespace IntelliCampus.Shared.Dtos.ChatMessage;

public class ChatMessageDto
{
    public int MessageId { get; set; }
    public string Content { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string SenderId { get; set; } = string.Empty;
    public string? SenderName { get; set; }
    public string RecipientId { get; set; } = string.Empty;
    public string? RecipientName { get; set; }
    public string? GroupName { get; set; }
    public bool IsEdited { get; set; }
    public bool IsPinned { get; set; }
}
