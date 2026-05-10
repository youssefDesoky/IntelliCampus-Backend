using IntelliCampus.Domain.Entities.Enums;

namespace IntelliCampus.Domain.Entities;

public class Material
{
    public int MaterialId { get; set; }
    public MaterialType Type { get; set; }
    public DateTime UploadDate { get; set; }
    public string Title { get; set; } = null!;
    public long? FileSize { get; set; }
    public string? FileUrl { get; set; }
    public int? CourseId { get; set; }
    public int? FolderId { get; set; }

    // Navigation properties
    public Course? Course { get; set; }
    public MaterialFolder? Folder { get; set; }
    public ICollection<InstructorMaterial> InstructorMaterials { get; set; } = [];
}
