using IntelliCampus.Domain.Entities.Enums;

namespace IntelliCampus.Domain.Entities;

public class Attendance
{
    public int AttendanceId { get; set; }
    public AttendanceStatus Status { get; set; }
    public DateTime Date { get; set; }
    public int SessionId { get; set; }
    public int StudentId { get; set; }

    // Navigation properties
    public Session Session { get; set; } = null!;
    public Student Student { get; set; } = null!;
}
