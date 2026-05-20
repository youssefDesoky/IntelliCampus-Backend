using IntelliCampus.shared.Dtos.Attendance;

namespace IntelliCampus.Service_Abstraction;

public interface IAttendanceService
{
    Task RecordAsync(int instructorId, RecordAttendanceDto dto);

    Task<IEnumerable<SessionDto>> GetByStudentAndCourseAsync(int studentId, int courseId);

    Task<AttendanceReportDto> GenerateReportAsync(int classId, int instructorId);

    Task<decimal> GetAttendancePercentageAsync(int studentId, int courseId);
}
