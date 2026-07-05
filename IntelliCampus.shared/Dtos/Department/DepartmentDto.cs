namespace IntelliCampus.Shared.Dtos.Department;

public class DepartmentDto
{
    public int DepartmentId { get; set; }
    public string DepartmentName { get; set; } = null!;
    public string? DepartmentNameAr { get; set; }
    public string? Description { get; set; }
    public string? DescriptionAr { get; set; }
    public int? InstructorId { get; set; }
    public string? HeadInstructorName { get; set; }
    public int? FacultyId { get; set; }
    public string? FacultyName { get; set; }
    public int? MaxCapacity { get; set; }
    public int CourseCount { get; set; }
}
