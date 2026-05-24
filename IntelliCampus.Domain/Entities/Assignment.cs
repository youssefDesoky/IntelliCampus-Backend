namespace IntelliCampus.Domain.Entities;

public class Assignment
{
    public int AssignmentId { get; set; }

    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? FullInstructions { get; set; }

    public DateTime DueDate { get; set; }

    // Max grade for the assignment
    public decimal MaxGrade { get; set; }

    // FK to the course this assignment belongs to
    public int CourseId { get; set; }

    // Navigation properties
    public Course Course { get; set; } = null!;
    public ICollection<StudentAssignment> StudentAssignments { get; set; } = [];
    public ICollection<AssignmentAttachment> Attachments { get; set; } = [];
}