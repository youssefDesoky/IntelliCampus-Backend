namespace IntelliCampus.Shared.Dtos.InstructorAnalytics;

public class CourseWorkBreakdownDto
{
    public decimal TotalMarks { get; set; }
    public List<CourseWorkBreakdownItemDto> Breakdown { get; set; } = [];
    public decimal UndeclaredMarks { get; set; }
}
