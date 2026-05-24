using IntelliCampus.Domain.Entities.Enums;

namespace IntelliCampus.shared.Dtos.Attendance;

public class RecordAttendanceDto
{
    public int SessionId { get; set; }

    public List<AttendanceEntry> Records { get; set; } = [];
}

public class AttendanceEntry
{
    public string StudentCode { get; set; } = string.Empty;

    public AttendanceStatus Status { get; set; }
}
