namespace IntelliCampus.Shared.Dtos.Bylaw;

public class BylawDto
{
    public int BylawId { get; set; }
    public string Name { get; set; } = null!;
    public int Version { get; set; }
    public string? Description { get; set; }
    public string? FileUrl { get; set; }
    public string? FileName { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public int? UploadedByAdminId { get; set; }
    public string? UploadedByAdminName { get; set; }
    public int? StudentCount { get; set; }
    public List<GradeScaleItemDto>? GradeScales { get; set; }
}

public class GradeScaleItemDto
{
    public string GradeLetter { get; set; } = null!;
    public decimal MinPercentage { get; set; }
    public decimal GpaValue { get; set; }
    public int SortOrder { get; set; }
}
