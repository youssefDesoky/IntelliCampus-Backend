using IntelliCampus.Domain.Entities.Enums;

namespace IntelliCampus.shared.Dtos.Attendance;

public class RecordAttendanceDto
{
    public int SessionId { get; set; }

    public List<StudentAttendanceRecord> Records { get; set; } = [];
}

public class StudentAttendanceRecord
{
    public int StudentId { get; set; }

    public AttendanceStatus Status { get; set; }
}
