using IntelliCampus.DAL.Entities.Enums;

namespace IntelliCampus.DAL.Entities;

public class Reminder
{
    public int ReminderId { get; set; }
    public ReminderType Type { get; set; }
    public string Title { get; set; } = null!;
    public DateTime Date { get; set; }
    public int StudentId { get; set; }

    // Navigation properties
    public Student Student { get; set; } = null!;
}
