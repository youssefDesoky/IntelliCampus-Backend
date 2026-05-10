namespace IntelliCampus.Domain.Entities;

public class StudentAssignment
{
    public int StudentId { get; set; }
    public int AssignmentId { get; set; }
    public decimal? Score { get; set; }
    public DateTime? SubmittedAt { get; set; }

    // Navigation properties
    public Student Student { get; set; } = null!;
    public Assignment Assignment { get; set; } = null!;
}
