using IntelliCampus.Domain.Entities.Enums;

namespace IntelliCampus.Shared.Params;

public class CourseQueryParams
{
    public int? StudentId { get; set; }
    public int? InstructorId { get; set; }
    public int? CourseId { get; set; }
    public int? DepartmentId { get; set; }
    public int? FacultyId { get; set; }
    public string? Status { get; set; }
    public string? Search { get; set; }
    public bool IncludeCourses { get; set; }
    public bool IsActiveOnly { get; set; }
    public int? ExcludeInstructorId { get; set; }

    public List<StudentCourseStatus>? StudentStatuses => Status?.ToLowerInvariant() switch
    {
        "inprogress" => [StudentCourseStatus.Registered, StudentCourseStatus.InProgress],
        "completed" => [StudentCourseStatus.Completed, StudentCourseStatus.Failed],
        "failed" => [StudentCourseStatus.Failed],
        _ => null
    };

    private int _pageIndex = 1;

    public int PageIndex
    {
        get => _pageIndex;
        set => _pageIndex = (value <= 0) ? 1 : value;
    }

    private const int DefaultPageSize = 10;
    private const int MaxPageSize = 200;
    private int _pageSize = DefaultPageSize;

    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = (value <= 0) ? DefaultPageSize : (value > MaxPageSize ? MaxPageSize : value);
    }

}
