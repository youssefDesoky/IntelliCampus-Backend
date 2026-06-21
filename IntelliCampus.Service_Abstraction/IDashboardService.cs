using IntelliCampus.Shared.Dtos.Dashboard;

namespace IntelliCampus.Service_Abstraction;

public interface IDashboardService
{
    Task<DashboardStatsDto> GetStatsAsync();
}
