using IntelliCampus.DAL.Entities.Enums;

namespace IntelliCampus.BLL.Dtos.Material;

public class MaterialDto
{
    public int MaterialId { get; set; }
    public string Title { get; set; } = null!;
    public string? Description { get; set; }
    public MaterialType Type { get; set; }
    public string TypeName => Type.ToString();
    public DateTime UploadDate { get; set; }
    public string? FileUrl { get; set; }
    public int? CourseId { get; set; }
    public string? CourseName { get; set; }
    public int? FolderId { get; set; }
    public string? FolderName { get; set; }
}
