namespace IntelliCampus.Shared.Dtos.ExamScheduling;

public class ConflictInfoDto
{
    public int StudentId { get; set; }
    public string StudentName { get; set; } = null!;
    public int ConflictingCourseId { get; set; }
    public string ConflictingCourseName { get; set; } = null!;
    public DateTime ExamDate { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
}
