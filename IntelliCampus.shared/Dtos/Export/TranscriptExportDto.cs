namespace IntelliCampus.Shared.Dtos.Export;

public class TranscriptExportDto
{
    public string StudentName { get; set; } = string.Empty;
    public string StudentCode { get; set; } = string.Empty;
    public string? Faculty { get; set; }
    public int? Level { get; set; }
    public string? Department { get; set; }
    public List<TranscriptCourseItem> Courses { get; set; } = [];
}

public class TranscriptCourseItem
{
    public string CourseCode { get; set; } = string.Empty;
    public string CourseName { get; set; } = string.Empty;
    public int CreditHours { get; set; }
    public string Coursework { get; set; } = "-";
    public string TotalGrade { get; set; } = "-";
    public string Letter { get; set; } = "-";
}
