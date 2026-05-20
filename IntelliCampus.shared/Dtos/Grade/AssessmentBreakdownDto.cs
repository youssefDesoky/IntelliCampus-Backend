namespace IntelliCampus.Shared.Dtos.Grade;

public class AssessmentBreakdownDto
{
    public string Category { get; set; } = string.Empty;
    public decimal TotalScore { get; set; }
    public decimal TotalMaxScore { get; set; }
    public decimal TotalWeight { get; set; }
    public decimal Percent { get; set; }
    public string Status { get; set; } = string.Empty;
}
