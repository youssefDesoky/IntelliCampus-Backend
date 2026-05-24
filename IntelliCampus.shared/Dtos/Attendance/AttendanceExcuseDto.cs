using System;
using IntelliCampus.Domain.Entities.Enums;

namespace IntelliCampus.shared.Dtos.Attendance;

public class AttendanceExcuseDto
{
    public int          ExcuseId             { get; set; }
    public string       StudentCode          { get; set; } = string.Empty;
    public string?      StudentName          { get; set; }
    public int          SessionId            { get; set; }
    public string?      Reason               { get; set; }
    public ExcuseStatus Status               { get; set; }
    public DateTime     CreatedAt            { get; set; }
    public string?      DocumentUrl          { get; set; }  // pre-built URL for download
    public string?      DocumentOriginalName { get; set; }
}
