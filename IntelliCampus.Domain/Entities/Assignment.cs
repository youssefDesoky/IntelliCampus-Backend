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

    // FK to the class this assignment belongs to
    public int ClassId { get; set; }

    // Navigation properties
    public Class Class { get; set; } = null!;
    public ICollection<StudentAssignment> StudentAssignments { get; set; } = [];
    public ICollection<AssignmentAttachment> Attachments { get; set; } = [];
}
