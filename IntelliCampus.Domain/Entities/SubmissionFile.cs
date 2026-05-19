namespace IntelliCampus.Domain.Entities;

public class SubmissionFile
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public long Size { get; set; }
    public string Url { get; set; } = string.Empty;

    public int StudentAssignmentId { get; set; }

    // Navigation
    public StudentAssignment StudentAssignment { get; set; } = null!;
}
