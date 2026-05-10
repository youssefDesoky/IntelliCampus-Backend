namespace IntelliCampus.Domain.Entities;

public class MaterialFolder
{
    public int MaterialFolderId { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public int CourseId { get; set; }
    public int CreatedByInstructorId { get; set; }
    public DateTime CreatedAt { get; set; }
    public int DisplayOrder { get; set; }

    // Navigation properties
    public Course Course { get; set; } = null!;
    public Instructor CreatedByInstructor { get; set; } = null!;
    public ICollection<Material> Materials { get; set; } = [];
}
