namespace IntelliCampus.Shared.Dtos.Material;

public class CreateMaterialFolderDto
{
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public int CourseId { get; set; }
    public int? DisplayOrder { get; set; }
}
