namespace IntelliCampus.Domain.Entities;

public class Announcement
{
    public int AnnouncementId { get; set; }
    public int CourseId { get; set; }
    public int SenderId { get; set; }
    public string Content { get; set; } = null!;
    public bool IsPinned { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public Course Course { get; set; } = null!;
    public User Sender { get; set; } = null!;
    public ICollection<AnnouncementAttachment> Attachments { get; set; } = [];
    public ICollection<AnnouncementComment> Comments { get; set; } = [];
}
