using IntelliCampus.Domain.Entities.Enums;

namespace IntelliCampus.Shared.Params;

public class CourseQueryParams
{
    public int? StudentId { get; set; }
    public int? InstructorId { get; set; }
    public int? CourseId { get; set; }
    public int? DepartmentId { get; set; }
    public string? Status { get; set; }
    public string? Search { get; set; }
    public bool IncludeCourses { get; set; }
    public bool IsActiveOnly { get; set; }

    public List<StudentCourseStatus>? StudentStatuses => Status?.ToLowerInvariant() switch
    {
        "inprogress" => [StudentCourseStatus.Registered, StudentCourseStatus.InProgress],
        "completed" => [StudentCourseStatus.Completed, StudentCourseStatus.Failed],
        "failed" => [StudentCourseStatus.Failed],
        _ => null
    };
}
