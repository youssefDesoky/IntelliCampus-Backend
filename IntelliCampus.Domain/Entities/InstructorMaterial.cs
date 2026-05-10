namespace IntelliCampus.Domain.Entities;

public class InstructorMaterial
{
    public int InstructorId { get; set; }
    public int MaterialId { get; set; }

    // Navigation properties
    public Instructor Instructor { get; set; } = null!;
    public Material Material { get; set; } = null!;
}
