using IntelliCampus.DAL.Entities.Enums;

namespace IntelliCampus.BLL.Dtos.Class;

public class CreateClassDto
{
    public ClassType ClassType { get; set; }
    public DayOfWeekEnum? Day { get; set; }
    public TimeSpan? StartTime { get; set; }
    public TimeSpan? EndTime { get; set; }
    public string? Room { get; set; }
    public int CourseId { get; set; }
    public int? InstructorId { get; set; }
}
