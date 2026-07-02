namespace IntelliCampus.Shared.Dtos.Grade;

public class InstructorAssessmentDto
{
    public int AssessmentId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public double Score { get; set; }
    public double MaxScore { get; set; }
    public double Weight { get; set; }
    public double Percent { get; set; }
}
