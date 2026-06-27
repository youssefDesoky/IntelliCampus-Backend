using IntelliCampus.Domain.Entities.Enums;
using IntelliCampus.shared.Dtos.Attendance;

namespace IntelliCampus.Service_Abstraction;

public interface IAttendanceExcuseService
{
    Task<AttendanceExcuseDto> SubmitAsync(int studentId, int courseId, SubmitExcuseFormDto dto, CancellationToken ct = default);

    Task<IEnumerable<AttendanceExcuseDto>> GetByStudentAsync(int studentId);

    Task<IEnumerable<AttendanceExcuseDto>> GetBySessionAsync(int sessionId, int instructorId);

    Task<IEnumerable<AttendanceExcuseDto>> GetByCourseAsync(int courseId, int instructorId);

    Task<AttendanceExcuseDto> UpdateStatusAsync(int excuseId, ExcuseStatus status, int instructorId);
}
