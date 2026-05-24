using IntelliCampus.shared.Dtos.Attendance;

namespace IntelliCampus.Service_Abstraction;

public interface IAttendanceService
{
    Task<QrTokenDto> GenerateQrAsync(int studentId);

    Task<AttendanceResultDto> ScanQrAsync(int instructorId, ScanQrDto dto);

    Task<AttendanceResultDto> RecordManualAsync(int instructorId, ManualAttendanceDto dto);

    Task RecordAsync(int instructorId, RecordAttendanceDto dto);

    Task<IEnumerable<SessionDto>> GetByStudentAndCourseAsync(int studentId, int courseId);

    Task<AttendanceReportDto> GenerateReportAsync(int classId, int instructorId);

    Task<decimal> GetAttendancePercentageAsync(int studentId, int courseId);
}
