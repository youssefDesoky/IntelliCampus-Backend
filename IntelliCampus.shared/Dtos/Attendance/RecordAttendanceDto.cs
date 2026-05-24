using IntelliCampus.Domain.Entities.Enums;

namespace IntelliCampus.shared.Dtos.Attendance;

public class RecordAttendanceDto
{
    public int SessionId { get; set; }

    public List<AttendanceEntry> Records { get; set; } = [];
}

public class AttendanceEntry
{
    public int StudentId { get; set; }

    public AttendanceStatus Status { get; set; }
}
