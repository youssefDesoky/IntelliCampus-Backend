namespace IntelliCampus.Domain.Entities;

public class AnnouncementComment
{
    public int AnnouncementCommentId { get; set; }
    public int AnnouncementId { get; set; }
    public int UserId { get; set; }
    public string Content { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public Announcement Announcement { get; set; } = null!;
    public User User { get; set; } = null!;
}
