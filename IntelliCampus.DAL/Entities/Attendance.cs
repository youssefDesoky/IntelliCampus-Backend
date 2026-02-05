using IntelliCampus.DAL.Entities.Enums;

namespace IntelliCampus.DAL.Entities;

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
