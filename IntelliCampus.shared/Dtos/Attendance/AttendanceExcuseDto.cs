using System;
using IntelliCampus.Domain.Entities.Enums;

namespace IntelliCampus.shared.Dtos.Attendance;

public class AttendanceExcuseDto
{
    public int          ExcuseId             { get; set; }
    public int          StudentId            { get; set; }
    public string?      StudentName          { get; set; }
    public int          SessionId            { get; set; }
    public string?      Reason               { get; set; }
    public ExcuseStatus Status               { get; set; }
    public DateTime     CreatedAt            { get; set; }
    public string?      DocumentUrl          { get; set; }  // pre-built URL for download
    public string?      DocumentOriginalName { get; set; }
}
