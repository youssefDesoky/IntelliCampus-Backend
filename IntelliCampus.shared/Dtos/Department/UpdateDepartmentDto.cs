namespace IntelliCampus.Shared.Dtos.Department;

public class UpdateDepartmentDto
{
    public string? DepartmentName { get; set; }
    public string? DepartmentNameAr { get; set; }
    public string? Description { get; set; }
    public string? DescriptionAr { get; set; }
    public int? InstructorId { get; set; }
    public int? FacultyId { get; set; }
    public int? MaxCapacity { get; set; }
}
