namespace IntelliCampus.Shared.Dtos.Dashboard;

public class StudentDashboardDto
{
    public StudentDashboardStatsDto Stats { get; set; } = new();
    public List<LatestNewsItemDto> LatestNews { get; set; } = [];
    public List<AttendanceTrendPointDto> AttendanceTrend { get; set; } = [];
    public List<GpaTrendPointDto> GpaTrend { get; set; } = [];
}

public class StudentDashboardStatsDto
{
    public int ActiveCourses { get; set; }
    public double AttendanceRate { get; set; }
    public double CurrentGpa { get; set; }
}

public class LatestNewsItemDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Course { get; set; } = string.Empty;
    public string Kind { get; set; } = "Course";
    public DateTime Date { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class AttendanceTrendPointDto
{
    public string Week { get; set; } = string.Empty;
    public double Attendance { get; set; }
}

public class GpaTrendPointDto
{
    public string Semester { get; set; } = string.Empty;
    public double Gpa { get; set; }
}
