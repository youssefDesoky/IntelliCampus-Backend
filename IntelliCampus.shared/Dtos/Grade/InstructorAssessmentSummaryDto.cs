namespace IntelliCampus.Shared.Dtos.Grade;

public class InstructorAssessmentSummaryDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public double MaxScore { get; set; }
    public double? Average { get; set; }
    public int Submissions { get; set; }
}
