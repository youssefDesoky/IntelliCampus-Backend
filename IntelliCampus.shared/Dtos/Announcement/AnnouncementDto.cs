namespace IntelliCampus.Shared.Dtos.Announcement;

public class AnnouncementDto
{
    public int Id { get; set; }
    public string CourseId { get; set; } = null!;
    public SenderDto Sender { get; set; } = null!;
    public DateTime Date { get; set; }
    public string Content { get; set; } = null!;
    public bool IsPinned { get; set; }
    public List<AttachmentDto> Attachments { get; set; } = [];
    public List<CommentDto> Comments { get; set; } = [];
    public int CommentCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
