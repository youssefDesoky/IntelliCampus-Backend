namespace IntelliCampus.Shared.Dtos.Export;

public class ScheduleExportDto
{
    public string StudentName { get; set; } = string.Empty;
    public string StudentCode { get; set; } = string.Empty;
    public string Title { get; set; } = "Weekly Schedule";
    public List<ScheduleItemExportDto> Items { get; set; } = [];
}

public class ScheduleItemExportDto
{
    public string Day { get; set; } = string.Empty;
    public string StartTime { get; set; } = string.Empty;
    public string EndTime { get; set; } = string.Empty;
    public string CourseName { get; set; } = string.Empty;
    public string? CourseNameAr { get; set; }
    public string Type { get; set; } = string.Empty;
    public string? Location { get; set; }
    public string? LocationAr { get; set; }
    public string? InstructorName { get; set; }
    public string? InstructorNameAr { get; set; }
}
