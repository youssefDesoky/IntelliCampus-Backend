namespace IntelliCampus.Shared.Dtos.InstructorAnalytics;

public class StudentScoreHeatmapRowDto
{
    public string Student { get; set; } = string.Empty;
    public Dictionary<string, double> Scores { get; set; } = [];
}
