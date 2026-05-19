namespace IntelliCampus.Domain.Entities;

public class StudentAssignment
{
    public int StudentAssignmentId { get; set; }

    public int StudentId { get; set; }
    public int AssignmentId { get; set; }

    public string? Note { get; set; }

    public DateTime SubmittedAt { get; set; }
    public bool IsLate { get; set; }

    public decimal? Grade { get; set; }
    public string? Feedback { get; set; }
    public int? GradedByInstructorId { get; set; }
    public DateTime? GradedAt { get; set; }

    // Navigation
    public Student Student { get; set; } = null!;
    public Assignment Assignment { get; set; } = null!;
    public Instructor? GradedByInstructor { get; set; }
    public ICollection<SubmissionFile> Files { get; set; } = [];
}
