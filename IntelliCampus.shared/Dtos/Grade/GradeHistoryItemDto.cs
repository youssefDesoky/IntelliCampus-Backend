namespace IntelliCampus.Shared.Dtos.Grade;

public class GradeHistoryItemDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public decimal Score { get; set; }
    public decimal MaxScore { get; set; }
    public decimal Weight { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Date { get; set; } = string.Empty;
    public decimal Percent { get; set; }
}
