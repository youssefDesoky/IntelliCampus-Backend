using IntelliCampus.Domain.Entities.Enums;

namespace IntelliCampus.Shared.Dtos.Material;

public class CreateMaterialDto
{
    public string Title { get; set; } = null!;
    public MaterialType Type { get; set; }
    public int CourseId { get; set; }
    public int? FolderId { get; set; }
}
