using IntelliCampus.DAL.Entities.Enums;

namespace IntelliCampus.BLL.Dtos.Course;

public class CreateCourseDto
{
    public string CourseName { get; set; } = null!;
    public string? CourseNameAr { get; set; }
    public int CreditHours { get; set; }
}
