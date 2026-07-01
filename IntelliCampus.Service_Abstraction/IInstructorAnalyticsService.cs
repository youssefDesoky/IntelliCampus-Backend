using IntelliCampus.Shared.Dtos.InstructorAnalytics;

namespace IntelliCampus.Service_Abstraction;

public interface IInstructorAnalyticsService
{
    Task<CourseAnalyticsDto> GetCourseAnalyticsAsync(int courseId, int userId);
}
