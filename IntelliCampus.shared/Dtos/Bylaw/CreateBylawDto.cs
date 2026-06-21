namespace IntelliCampus.Shared.Dtos.Bylaw;

public class CreateBylawDto
{
    public string Name { get; set; } = null!;
    public string? NameAr { get; set; }
    public string? Description { get; set; }
    public string? DescriptionAr { get; set; }
    public string Type { get; set; } = null!;
    public List<GradeScaleItemDto>? GradeScales { get; set; }
    public List<LevelScaleItemDto>? LevelScales { get; set; }
    public int? MinHoursToChooseDepartment { get; set; }
    public int? MinHoursToChooseSpecialization { get; set; }
}
