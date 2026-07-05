namespace IntelliCampus.Shared.Dtos.Dashboard;

public class AdminDashboardDto
{
    public AdminStatsDto Stats { get; set; } = new();
    public List<LatestNewsItemDto> LatestNews { get; set; } = [];
    public AdminChartsDto Charts { get; set; } = new();
    public AdminSnapshotDto Snapshot { get; set; } = new();
}

public class AdminStatsDto
{
    public int TotalStudents { get; set; }
    public int Instructors { get; set; }
    public int Courses { get; set; }
    public int Departments { get; set; }
    public int ActiveClasses { get; set; }
    public int Rooms { get; set; }
}

public class AdminChartsDto
{
    public List<AttendanceTrendPointDto> AttendanceTrend { get; set; } = [];
    public List<GradeDistributionPointDto> GradeDistribution { get; set; } = [];
    public List<TopCourseDto> TopCourses { get; set; } = [];
    public List<DepartmentStatusDto> DepartmentStatus { get; set; } = [];
    public List<CourseStatusPointDto> CourseStatusBreakdown { get; set; } = [];
    public List<ProbationDeptPointDto> ProbationHeatmap { get; set; } = [];
}

public class ProbationDeptPointDto
{
    public string Department { get; set; } = string.Empty;
    public int Level { get; set; }
    public int ProbationCount { get; set; }
    public int TotalStudents { get; set; }
    public double ProbationRate { get; set; }
}

public class GradeDistributionPointDto
{
    public string Name { get; set; } = string.Empty;
    public int Value { get; set; }
}

public class TopCourseDto
{
    public string Course { get; set; } = string.Empty;
    public int Enrolled { get; set; }
}

public class DepartmentStatusDto
{
    public string Dept { get; set; } = string.Empty;
    public int Active { get; set; }
    public int Completed { get; set; }
    public int Upcoming { get; set; }
}

public class CourseStatusPointDto
{
    public string Name { get; set; } = string.Empty;
    public int Value { get; set; }
}

public class AdminSnapshotDto
{
    public double PassRate { get; set; }
    public double CourseCompletion { get; set; }
    public double StudentRetention { get; set; }
    public double AverageGpa { get; set; }
    public int ProbationCount { get; set; }
}

public class PublishNewsDto
{
    public string Title { get; set; } = string.Empty;
}
