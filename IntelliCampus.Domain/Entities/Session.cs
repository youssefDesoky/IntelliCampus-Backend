namespace IntelliCampus.Domain.Entities;

public class Session
{
    public int SessionId { get; set; }
    public string? Topic { get; set; }
    public DateTime Date { get; set; }
    public int ClassId { get; set; }
    
    public TimeOnly? StartTime  { get; set; }
    public TimeOnly? EndTime    { get; set; }
    public IntelliCampus.Domain.Entities.Enums.SessionType SessionType { get; set; } = IntelliCampus.Domain.Entities.Enums.SessionType.Lecture;

    // Navigation properties
    public Class Class { get; set; } = null!;
    public ICollection<Note> Notes { get; set; } = [];
    public ICollection<Attendance> Attendances { get; set; } = [];
    public ICollection<AttendanceExcuse> Excuses { get; set; } = [];
}
