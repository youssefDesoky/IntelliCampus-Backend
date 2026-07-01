using IntelliCampus.Shared.Dtos.Dashboard;

namespace IntelliCampus.Service_Abstraction;

public interface IDashboardService
{
    Task<DashboardStatsDto> GetStatsAsync();
    Task<StudentDashboardDto> GetStudentDashboardAsync(int studentId);
    Task<AdminDashboardDto> GetAdminDashboardAsync();
    Task<InstructorDashboardDto> GetInstructorDashboardAsync(int instructorId);
    Task<LatestNewsItemDto> PublishNewsAsync(int senderId, string title);
    Task<LatestNewsItemDto> UpdateNewsAsync(int id, int senderId, string title);
    Task DeleteNewsAsync(int id);
}
