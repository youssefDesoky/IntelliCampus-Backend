namespace IntelliCampus.Shared.Dtos.Bylaw;

public class MapBylawCourseDto
{
    public int CourseId { get; set; }
    public string CourseType { get; set; } = null!;
    public int? CreditHours { get; set; }
    public List<int>? AllowedDepartmentIds { get; set; }
}
