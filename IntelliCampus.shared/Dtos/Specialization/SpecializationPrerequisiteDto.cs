namespace IntelliCampus.Shared.Dtos.Specialization;

public class SpecializationPrerequisiteDto
{
    public int CourseId { get; set; }
    public string? CourseName { get; set; }
    public string? CourseCode { get; set; }
    public decimal MinGrade { get; set; }
}
