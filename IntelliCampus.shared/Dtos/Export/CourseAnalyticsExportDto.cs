using IntelliCampus.Shared.Dtos.InstructorAnalytics;

namespace IntelliCampus.Shared.Dtos.Export;

public class CourseAnalyticsExportDto
{
    public string CourseName { get; set; } = string.Empty;
    public string CourseCode { get; set; } = string.Empty;
    public string InstructorName { get; set; } = string.Empty;
    public List<AssessmentPerformanceItemDto> AssessmentPerformance { get; set; } = [];
    public List<SubmissionRateItemDto> SubmissionRate { get; set; } = [];
    public List<WeeklyAttendanceItemDto> WeeklyAttendance { get; set; } = [];
}
