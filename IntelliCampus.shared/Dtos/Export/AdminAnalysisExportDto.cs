namespace IntelliCampus.Shared.Dtos.Export;

public class AdminAnalysisExportDto
{
    public string InstitutionName { get; set; } = "IntelliCampus";
    public int TotalStudents { get; set; }
    public int TotalInstructors { get; set; }
    public int TotalCourses { get; set; }
    public int TotalDepartments { get; set; }
    public int TotalRooms { get; set; }
    public int TotalExams { get; set; }
    public int ActiveBylaws { get; set; }
    public List<DepartmentAnalysisItemDto> DepartmentBreakdown { get; set; } = [];
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
}

public class DepartmentAnalysisItemDto
{
    public string DepartmentName { get; set; } = string.Empty;
    public int StudentCount { get; set; }
    public int InstructorCount { get; set; }
    public int CourseCount { get; set; }
}
