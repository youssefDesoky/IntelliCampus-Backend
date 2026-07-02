using IntelliCampus.Domain.Entities.Enums;

namespace IntelliCampus.Shared.Dtos.Class;

public class ClassDto
{
    public int ClassId { get; set; }
    public string? GroupCode { get; set; }
    public string? GroupCodeAr { get; set; }
    public ClassType ClassType { get; set; }
    public string ClassTypeName => ClassType.ToString();
    public string? ClassTypeAr { get; set; }
    public DayOfWeekEnum? Day { get; set; }
    public string? DayName => Day?.ToString();
    public string? DayNameAr { get; set; }
    public TimeSpan? StartTime { get; set; }
    public TimeSpan? EndTime { get; set; }
    public int? RoomId { get; set; }
    public string? RoomName { get; set; }
    public string? RoomNameAr { get; set; }
    public int? Capacity { get; set; }
    public int EnrolledCount { get; set; }
    public int CourseId { get; set; }
    public string CourseName { get; set; } = null!;
    public string? CourseNameAr { get; set; }
    public int? InstructorId { get; set; }
    public string? InstructorName { get; set; }
    public string? InstructorNameAr { get; set; }
}
