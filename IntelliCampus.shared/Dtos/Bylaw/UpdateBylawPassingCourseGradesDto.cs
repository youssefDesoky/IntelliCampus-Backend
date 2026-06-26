namespace IntelliCampus.Shared.Dtos.Bylaw;

public class UpdateBylawPassingCourseGradesDto
{
    public decimal? MinPassingCourseworkGrade { get; set; }
    public decimal? MinPassingFinalExamGrade { get; set; }
    public string? MaxGradeOnRetake { get; set; }
}
