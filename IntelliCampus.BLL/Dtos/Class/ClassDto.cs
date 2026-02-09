using IntelliCampus.DAL.Entities.Enums;

namespace IntelliCampus.BLL.Dtos.Class;

public class ClassDto
{
    public int ClassId { get; set; }
    public string? GroupCode { get; set; }
    public ClassType ClassType { get; set; }
    public string ClassTypeName => ClassType.ToString();
    public DayOfWeekEnum? Day { get; set; }
    public string? DayName => Day?.ToString();
    public TimeSpan? StartTime { get; set; }
    public TimeSpan? EndTime { get; set; }
    public string? Room { get; set; }
    public int CourseId { get; set; }
    public string CourseName { get; set; } = null!;
    public int? InstructorId { get; set; }
    public string? InstructorName { get; set; }
}
