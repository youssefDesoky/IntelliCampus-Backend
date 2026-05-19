namespace IntelliCampus.Shared.Dtos.Assignment;

public class SubmitAssignmentDto
{
    public int AssignmentId { get; set; }
    public string? FileUrl { get; set; }
    public string? Notes { get; set; }
}
