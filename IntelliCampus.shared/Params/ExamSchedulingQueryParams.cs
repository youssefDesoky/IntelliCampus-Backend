namespace IntelliCampus.Shared.Params;

public class ExamSchedulingQueryParams
{
    public int? CourseId { get; set; }
    public DateTime? Date { get; set; }
    public TimeSpan? StartTime { get; set; }
    public TimeSpan? EndTime { get; set; }
    public int? ExcludeExamId { get; set; }
}
