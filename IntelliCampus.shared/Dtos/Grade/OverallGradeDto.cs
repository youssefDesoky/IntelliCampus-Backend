namespace IntelliCampus.Shared.Dtos.Grade;

public class OverallGradeDto
{
    public decimal Percent { get; set; }
    public string Letter { get; set; } = string.Empty;
    public decimal Gpa { get; set; }
}
