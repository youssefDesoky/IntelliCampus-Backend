namespace IntelliCampus.Shared.Dtos.Faculty;

public class FacultyDto
{
    public int FacultyId { get; set; }
    public string FacultyName { get; set; } = null!;
    public string? FacultyNameAr { get; set; }
    public string FacultyCode { get; set; } = null!;
    public string? Description { get; set; }
    public List<string> DepartmentNames { get; set; } = [];
}
