namespace IntelliCampus.Shared.Dtos.Material;

public class CourseMaterialsDto
{
    public int CourseId { get; set; }
    public string CourseName { get; set; } = null!;
    public IEnumerable<MaterialFolderWithMaterialsDto> Folders { get; set; } = [];
    public IEnumerable<MaterialDto> UnorganizedMaterials { get; set; } = [];
}

public class MaterialFolderWithMaterialsDto
{
    public int MaterialFolderId { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public int DisplayOrder { get; set; }
    public IEnumerable<MaterialDto> Materials { get; set; } = [];
}
