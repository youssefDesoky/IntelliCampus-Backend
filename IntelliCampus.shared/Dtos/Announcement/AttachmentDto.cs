namespace IntelliCampus.Shared.Dtos.Announcement;

public class AttachmentDto
{
    public string Id { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string Url { get; set; } = null!;
    public string FileType { get; set; } = null!;
    public long FileSize { get; set; }
}
