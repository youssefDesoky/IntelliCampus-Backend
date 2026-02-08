using IntelliCampus.DAL.Entities.Enums;

namespace IntelliCampus.BLL.Dtos.Course;

public class CourseDto
{
    public int CourseId { get; set; }
    public string CourseName { get; set; } = null!;
    public string? CourseNameAr { get; set; }
    public int CreditHours { get; set; }
    public CourseStatus Status { get; set; }
    public string StatusName => Status.ToString();
    public int? DepartmentId { get; set; }
    public string? DepartmentName { get; set; }
    public int ClassCount { get; set; }
}
