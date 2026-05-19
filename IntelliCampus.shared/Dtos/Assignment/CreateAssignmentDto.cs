namespace IntelliCampus.Shared.Dtos.Assignment;

public class CreateAssignmentDto
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime DueDate { get; set; }
    public decimal MaxGrade { get; set; }
    public int ClassId { get; set; }
}
