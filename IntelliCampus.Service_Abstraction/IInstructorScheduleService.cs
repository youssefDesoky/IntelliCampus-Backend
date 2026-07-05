using IntelliCampus.Domain.Entities.Enums;
using IntelliCampus.Shared.Dtos.Schedule;
using IntelliCampus.Shared.Params;

namespace IntelliCampus.Service_Abstraction;

public interface IInstructorScheduleService
{
    Task<IEnumerable<ScheduleDto>> GetMyScheduleAsync(int userId, ScheduleQueryParams queryParams);
    Task<IEnumerable<ScheduleDto>> GetScheduleAsync(int instructorId, ScheduleQueryParams queryParams);
    Task<ScheduleDto> GetScheduleByIdAsync(int classId, int userId);
    Task<byte[]> ExportSchedulePdfAsync(int userId, ScheduleQueryParams queryParams);
}
