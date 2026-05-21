namespace IntelliCampus.Shared.Dtos.Assignment;

public class GradeInfoDto
{
    public decimal Score { get; set; }
    public decimal TotalPoints { get; set; }
    public string? Feedback { get; set; }
    public string? GradedBy { get; set; }
    public string? GradedAt { get; set; }
}
