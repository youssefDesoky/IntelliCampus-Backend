using IntelliCampus.Domain.Entities.Enums;

namespace IntelliCampus.Domain.Entities;

public class GradeComplaint
{
    public int ComplaintId { get; set; }
    public int GradeId { get; set; }
    public int StudentId { get; set; }
    public string ComplaintType { get; set; } = string.Empty;
    public string Details { get; set; } = string.Empty;
    public ComplaintStatus Status { get; set; } = ComplaintStatus.Pending;
    public DateTime SubmittedAt { get; set; }
    public string? InstructorResponse { get; set; }

    // Navigation
    public Grade Grade { get; set; } = null!;
    public Student Student { get; set; } = null!;
}
