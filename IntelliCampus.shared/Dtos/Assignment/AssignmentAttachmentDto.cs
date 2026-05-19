namespace IntelliCampus.Shared.Dtos.Assignment;

public class AssignmentAttachmentDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public long Size { get; set; }
    public string Url { get; set; } = string.Empty;
}
