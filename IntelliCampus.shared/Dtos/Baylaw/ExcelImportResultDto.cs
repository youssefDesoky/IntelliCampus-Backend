namespace IntelliCampus.Shared.Dtos.Baylaw;

public class ExcelImportResultDto
{
    public int TotalRows { get; set; }
    public int SuccessCount { get; set; }
    public int FailCount { get; set; }
    public List<string> Errors { get; set; } = new();
}
