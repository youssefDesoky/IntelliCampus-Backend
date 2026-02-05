namespace IntelliCampus.DAL.Entities;

public class Instructor : User
{
    public int InstructorId { get; set; }
    public string? InstructorRole { get; set; }
    public string? Specialization { get; set; }
    public int? DepartmentId { get; set; }

    // Navigation properties
    public Department? Department { get; set; }
    public ICollection<InstructorMaterial> InstructorMaterials { get; set; } = [];
}
