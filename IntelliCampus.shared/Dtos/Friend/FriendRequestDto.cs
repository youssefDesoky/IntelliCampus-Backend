namespace IntelliCampus.Shared.Dtos.Friend;

public class FriendRequestDto
{
    public int FriendRequestId { get; set; }
    public int SenderId { get; set; }
    public string SenderName { get; set; } = string.Empty;
    public string? SenderProfileImage { get; set; }
    public int RecipientId { get; set; }
    public string RecipientName { get; set; } = string.Empty;
    public string? RecipientProfileImage { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
