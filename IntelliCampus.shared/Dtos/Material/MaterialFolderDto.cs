namespace IntelliCampus.Shared.Dtos.Material;

public class MaterialFolderDto
{
    public int MaterialFolderId { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public int CourseId { get; set; }
    public string CourseName { get; set; } = null!;
    public int CreatedByInstructorId { get; set; }
    public string CreatedByInstructorName { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public int DisplayOrder { get; set; }
    public int MaterialCount { get; set; }
}
