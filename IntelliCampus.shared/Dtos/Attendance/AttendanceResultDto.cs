using IntelliCampus.Domain.Entities.Enums;

namespace IntelliCampus.shared.Dtos.Attendance;

public class AttendanceResultDto
{
    public string StudentName { get; set; } = string.Empty;
    public string StudentCode { get; set; } = string.Empty;
    public AttendanceStatus Status { get; set; }
    public DateTime RecordedAt { get; set; }
    public string Method { get; set; } = string.Empty;
}
