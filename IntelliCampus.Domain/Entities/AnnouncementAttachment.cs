namespace IntelliCampus.Domain.Entities;

public class AnnouncementAttachment
{
    public int AnnouncementAttachmentId { get; set; }
    public int AnnouncementId { get; set; }
    public string FileName { get; set; } = null!;
    public string FileUrl { get; set; } = null!;
    public string FileType { get; set; } = null!;
    public long FileSize { get; set; }

    public Announcement Announcement { get; set; } = null!;
}
