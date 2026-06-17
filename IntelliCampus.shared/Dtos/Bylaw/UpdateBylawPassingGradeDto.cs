namespace IntelliCampus.Shared.Dtos.Bylaw;

public class UpdateBylawPassingGradeDto
{
    public decimal? MinPassingGpa { get; set; }
    public string? MinPassingGradeLetter { get; set; }
    public int? MinPassingGradeSortOrder { get; set; }
}
