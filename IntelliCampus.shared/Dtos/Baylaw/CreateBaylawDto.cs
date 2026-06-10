namespace IntelliCampus.Shared.Dtos.Baylaw;

public class CreateBaylawDto
{
    public string Name { get; set; } = null!;
    public int Version { get; set; }
    public string? Description { get; set; }
    public List<GradeScaleItemDto>? GradeScales { get; set; }
}
