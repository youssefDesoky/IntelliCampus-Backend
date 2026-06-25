namespace IntelliCampus.Shared.Dtos.Grade;

public class InstructorCourseGradesDto
{
    public int CourseId { get; set; }
    public string CourseName { get; set; } = string.Empty;
    public string? CourseCode { get; set; }
    public InstructorCourseSummaryDto Summary { get; set; } = new();
    public List<InstructorAssessmentSummaryDto> Assessments { get; set; } = [];
    public List<InstructorStudentGradeDto> Students { get; set; } = [];
}
