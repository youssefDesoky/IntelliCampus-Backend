namespace IntelliCampus.Shared.Dtos.InstructorAnalytics;

public class CourseAnalyticsDto
{
    public List<AssessmentPerformanceItemDto> AssessmentPerformance { get; set; } = [];
    public List<SubmissionRateItemDto> SubmissionRate { get; set; } = [];
    public List<WeeklyAttendanceItemDto> WeeklyAttendance { get; set; } = [];
}
