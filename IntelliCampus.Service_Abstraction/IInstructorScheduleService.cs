using IntelliCampus.Domain.Entities.Enums;
using IntelliCampus.Shared.Dtos.Schedule;

namespace IntelliCampus.Service_Abstraction;

public interface IInstructorScheduleService
{
    Task<IEnumerable<ScheduleDto>> GetMyScheduleAsync(int userId, IReadOnlyCollection<ScheduleType>? types);
    Task<ScheduleDto> GetScheduleByIdAsync(int classId);
    Task<byte[]> ExportSchedulePdfAsync(int userId, IReadOnlyCollection<ScheduleType>? types);
}
