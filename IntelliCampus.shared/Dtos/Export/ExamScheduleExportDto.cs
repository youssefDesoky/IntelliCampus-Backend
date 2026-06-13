namespace IntelliCampus.Shared.Dtos.Export;

public class ExamScheduleExportDto
{
    public string StudentName { get; set; } = string.Empty;
    public string StudentCode { get; set; } = string.Empty;
    public string Title { get; set; } = "Exam Schedule";
    public List<ExamScheduleItem> Items { get; set; } = [];
}

public class ExamScheduleItem
{
    public string CourseCode { get; set; } = string.Empty;
    public string CourseName { get; set; } = string.Empty;
    public string Day { get; set; } = string.Empty;
    public string Date { get; set; } = string.Empty;
    public string StartTime { get; set; } = string.Empty;
    public string EndTime { get; set; } = string.Empty;
    public string Duration { get; set; } = string.Empty;
    public string? Location { get; set; }
    public string ExamType { get; set; } = string.Empty;
}
