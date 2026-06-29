using IntelliCampus.Domain.Entities.Enums;
using IntelliCampus.Shared.Dtos.Schedule;
using IntelliCampus.Shared.Params;

namespace IntelliCampus.Service_Abstraction;

public interface IScheduleService
{
    Task<ScheduleDto> GetByIdAsync(int scheduleId);
    Task<IEnumerable<ScheduleDto>> GetByStudentIdAsync(int studentId, ScheduleQueryParams? queryParams = null);

    Task<IEnumerable<ScheduleDto>> GetByStudentIdAndTypeAsync(int studentId, ScheduleType type, ScheduleQueryParams? queryParams = null);

    Task<IEnumerable<ScheduleDto>> GetByStudentIdAndTypesAsync(int studentId, ScheduleQueryParams queryParams);
    Task<byte[]> ExportSchedulePdfAsync(int studentId, ScheduleQueryParams queryParams);

    Task SyncFromCourseRegistrationAsync(int studentId, int classId);
    Task RemoveByStudentAndCourseAsync(int studentId, int courseId);
    Task SyncFromClassUpdateAsync(int classId);
}
