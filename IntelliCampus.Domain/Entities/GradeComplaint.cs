namespace IntelliCampus.Domain.Entities;

public class GradeComplaint
{
    public int ComplaintId { get; set; }
    public int GradeId { get; set; }
    public int StudentId { get; set; }
    public string ComplaintType { get; set; } = string.Empty;
    public string Details { get; set; } = string.Empty;
    public string Status { get; set; } = "Pending"; // Pending | Reviewed
    public DateTime SubmittedAt { get; set; }

    // Navigation
    public Grade Grade { get; set; } = null!;
    public Student Student { get; set; } = null!;
}
