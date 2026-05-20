using IntelliCampus.Domain.Entities.Enums;

namespace IntelliCampus.shared.Dtos.Attendance;

public class SessionDto
{
    public int      SessionId      { get; set; }
    public DateTime Date           { get; set; }
    public string?  StartTime      { get; set; }   // "10:00 AM"
    public string?  EndTime        { get; set; }   // "11:30 AM"
    public string?  Topic          { get; set; }
    public int      ClassId        { get; set; }
    public string?  ClassName      { get; set; }
    public SessionType SessionType { get; set; }
    public int      TotalStudents  { get; set; }
    public int      PresentCount   { get; set; }
}
