using IntelliCampus.Domain.Entities.Enums;
using IntelliCampus.Shared.Dtos.Schedule;

namespace IntelliCampus.Service_Abstraction;

public interface IScheduleService
{
    Task<ScheduleDto?> GetByIdAsync(int scheduleId);
    Task<IEnumerable<ScheduleDto>> GetByStudentIdAsync(int studentId);

    Task<IEnumerable<ScheduleDto>> GetByStudentIdAndTypeAsync(int studentId, ScheduleType type);

    Task<IEnumerable<ScheduleDto>> GetByStudentIdAndTypesAsync(int studentId, IReadOnlyCollection<ScheduleType> types);

    Task SyncFromCourseRegistrationAsync(int studentId, int classId);
    Task RemoveByStudentAndCourseAsync(int studentId, int courseId);
    Task SyncFromClassUpdateAsync(int classId);
}
