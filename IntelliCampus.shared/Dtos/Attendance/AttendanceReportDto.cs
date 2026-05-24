using System.Collections.Generic;

namespace IntelliCampus.shared.Dtos.Attendance;

public class AttendanceReportDto
{
    public int     ClassId                  { get; set; }
    public string? ClassName                { get; set; }
    public int     TotalSessions            { get; set; }
    public decimal OnTimePercentage         { get; set; }   // Present / Total
    public decimal NeedsImprovementPercentage { get; set; } // Late  / Total
    public int BelowThresholdCount { get; set; }
    public List<StudentAttendanceSummary> Students { get; set; } = [];
}

public class StudentAttendanceSummary
{
    public string StudentCode { get; set; } = string.Empty;
    public string? StudentName { get; set; }
    public int Present { get; set; }
    public int Absent { get; set; }
    public decimal AttendancePercentage { get; set; }
    public bool BelowThreshold { get; set; }
}
