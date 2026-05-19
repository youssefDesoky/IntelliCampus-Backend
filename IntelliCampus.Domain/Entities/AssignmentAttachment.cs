namespace IntelliCampus.Domain.Entities;

public class AssignmentAttachment
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public long Size { get; set; }
    public string Url { get; set; } = string.Empty;

    public int AssignmentId { get; set; }

    // Navigation
    public Assignment Assignment { get; set; } = null!;
}
