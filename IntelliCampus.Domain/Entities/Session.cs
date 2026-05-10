namespace IntelliCampus.Domain.Entities;

public class Session
{
    public int SessionId { get; set; }
    public string? Topic { get; set; }
    public DateTime Date { get; set; }
    public int ClassId { get; set; }

    // Navigation properties
    public Class Class { get; set; } = null!;
    public ICollection<Note> Notes { get; set; } = [];
    public ICollection<Attendance> Attendances { get; set; } = [];
}
