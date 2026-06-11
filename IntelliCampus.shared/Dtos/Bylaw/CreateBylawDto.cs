namespace IntelliCampus.Shared.Dtos.Bylaw;

public class CreateBylawDto
{
    public string Name { get; set; } = null!;
    public int Version { get; set; }
    public string? Description { get; set; }
    public List<GradeScaleItemDto>? GradeScales { get; set; }
}
