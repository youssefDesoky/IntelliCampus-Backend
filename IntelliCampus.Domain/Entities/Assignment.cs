namespace IntelliCampus.Domain.Entities;

public class Assignment
{
    public int AssignmentId { get; set; }
    public int TotalMarks { get; set; }
    public DateTime DueDate { get; set; }

    // Navigation properties
    public ICollection<StudentAssignment> StudentAssignments { get; set; } = new List<StudentAssignment>();
}
