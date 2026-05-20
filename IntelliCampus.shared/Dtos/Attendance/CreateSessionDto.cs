using System;
using IntelliCampus.Domain.Entities.Enums;

namespace IntelliCampus.shared.Dtos.Attendance;

public class CreateSessionDto
{
    public int         ClassId     { get; set; }
    public DateTime    Date        { get; set; }
    public TimeOnly?   StartTime   { get; set; }
    public TimeOnly?   EndTime     { get; set; }
    public string?     Topic       { get; set; }
    public SessionType SessionType { get; set; } = SessionType.Lecture;
}
