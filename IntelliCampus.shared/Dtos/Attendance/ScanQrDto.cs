using IntelliCampus.Domain.Entities.Enums;

namespace IntelliCampus.shared.Dtos.Attendance;

public class ScanQrDto
{
    public int SessionId { get; set; }
    public string QrPayload { get; set; } = string.Empty;
    public AttendanceStatus Status { get; set; } = AttendanceStatus.Present;
}
