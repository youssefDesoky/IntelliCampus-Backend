namespace IntelliCampus.Shared.Dtos.Assignment;

public class AssignmentDto
{
    public int AssignmentId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime DueDate { get; set; }
    public decimal MaxGrade { get; set; }
    public int ClassId { get; set; }
    public string? ClassName { get; set; }
    public int CourseId { get; set; }
    public string? CourseName { get; set; }
}
