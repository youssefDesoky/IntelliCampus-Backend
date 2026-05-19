namespace IntelliCampus.Shared.Dtos.Assignment;

public class SubmissionDto
{
    public int StudentAssignmentId { get; set; }
    public int StudentId { get; set; }
    public string? StudentName { get; set; }
    public int AssignmentId { get; set; }
    public string? AssignmentTitle { get; set; }
    public string? FileUrl { get; set; }
    public string? Notes { get; set; }
    public DateTime SubmittedAt { get; set; }
    public decimal? Grade { get; set; }
    public bool IsLate { get; set; }
}
