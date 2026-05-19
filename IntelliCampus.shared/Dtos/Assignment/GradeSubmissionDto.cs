namespace IntelliCampus.Shared.Dtos.Assignment;

public class GradeSubmissionDto
{
    public int StudentAssignmentId { get; set; }
    public decimal Score { get; set; }
    public string? Feedback { get; set; }
}
