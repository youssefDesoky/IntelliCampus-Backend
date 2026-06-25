using IntelliCampus.shared.Dtos.Attendance;
using IntelliCampus.shared.Pagination;
using IntelliCampus.Shared.Params;

namespace IntelliCampus.Service_Abstraction;

public interface IAttendanceService
{
    Task<SessionAttendanceDto> GetSessionAttendanceAsync(int sessionId, int instructorId);
    Task<QrTokenDto> GenerateQrAsync(int studentId);

    Task<AttendanceResultDto> ScanQrAsync(int instructorId, ScanQrDto dto);

    Task<AttendanceResultDto> RecordManualAsync(int instructorId, ManualAttendanceDto dto);

    Task RecordAsync(int instructorId, RecordAttendanceDto dto);

    Task<IEnumerable<SessionDto>> GetByStudentAndCourseAsync(int studentId, int courseId);
    Task<PaginatedResult<SessionDto>> GetByStudentAndCourseAsync(int studentId, int courseId, SessionQueryParams queryParams);

    Task<AttendanceReportDto> GenerateReportAsync(int classId, int instructorId);
    Task<PaginatedResult<AttendanceReportDto>> GenerateReportAsync(int classId, int instructorId, SessionQueryParams queryParams);

    Task<decimal> GetAttendancePercentageAsync(int studentId, int courseId);
}
