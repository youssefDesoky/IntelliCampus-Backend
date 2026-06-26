namespace IntelliCampus.Shared.Dtos.Grade;

public class InstructorStudentGradeDto
{
    public int StudentId { get; set; }
    public string StudentCode { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public List<InstructorAssessmentDto> Assessments { get; set; } = [];
    public double OverallPercent { get; set; }
    public string Letter { get; set; } = "-";
}
