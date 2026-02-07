using IntelliCampus.DAL.Entities.Enums;

namespace IntelliCampus.BLL.Dtos.Material;

public class CreateMaterialDto
{
    public string Title { get; set; } = null!;
    public string? Description { get; set; }
    public MaterialType Type { get; set; }
    public int CourseId { get; set; }
    public int? FolderId { get; set; }
}
