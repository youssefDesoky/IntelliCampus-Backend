namespace IntelliCampus.Domain.Entities;

public class ChatMessage
{
    public int MessageId { get; set; }
    public string Content { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = EgyptTime.Now;
    public string SenderId { get; set; } = string.Empty;
    public string RecipientId { get; set; } = string.Empty;
    public string? GroupName { get; set; }
    public bool IsEdited { get; set; }
    public bool IsPinned { get; set; }

    // Navigation properties
    public User Sender { get; set; } = null!;
    public User Recipient { get; set; } = null!;
}