namespace IntelliCampus.Shared.Dtos.Grade;

public class CourseGradeDto
{
    public OverallGradeDto OverallGrade { get; set; } = new();
    public List<AssessmentBreakdownDto> AssessmentBreakdown { get; set; } = [];
    public List<GradeHistoryItemDto> History { get; set; } = [];
}
