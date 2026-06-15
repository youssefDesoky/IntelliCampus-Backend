namespace IntelliCampus.Domain.Entities;

public class Specialization
{
    public int SpecializationId { get; set; }
    public string Name { get; set; } = null!;
    public string? NameAr { get; set; }
    public int DepartmentId { get; set; }

    public Department Department { get; set; } = null!;
    public ICollection<Student> Students { get; set; } = new List<Student>();
}
