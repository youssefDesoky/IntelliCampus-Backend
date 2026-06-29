namespace IntelliCampus.Shared.Dtos.Dashboard;

public class InstructorDashboardDto
{
    public InstructorStatsDto Stats { get; set; } = new();
    public List<LatestNewsItemDto> LatestNews { get; set; } = [];
    public List<AttendanceTrendPointDto> AttendanceTrend { get; set; } = [];
    public List<RadarDataPointDto> RadarData { get; set; } = [];
}

public class InstructorStatsDto
{
    public int ActiveCourses { get; set; }
    public int TotalStudents { get; set; }
    public double AverageAttendance { get; set; }
}

public class RadarDataPointDto
{
    public string Skill { get; set; } = string.Empty;
    public double Score { get; set; }
    public int FullMark { get; set; } = 100;
}