namespace IntelliCampus.Domain.Entities;

public class Instructor : User
{
    public int InstructorId { get; set; }
    public string? InstructorCode { get; set; }
    public string? InstructorRole { get; set; }
    public string? Specialization { get; set; }
    public int? DepartmentId { get; set; }
    public DateTime? HireDate { get; set; }

    // Navigation properties
    public Department? Department { get; set; }
    public ICollection<InstructorMaterial> InstructorMaterials { get; set; } = [];
    public ICollection<Class> Classes { get; set; } = [];
    public ICollection<MaterialFolder> CreatedFolders { get; set; } = [];
}
