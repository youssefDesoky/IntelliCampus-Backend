namespace IntelliCampus.Shared.Dtos.Course;

public class StudentAllCoursesDto
{
    public List<CourseDto> InProgress { get; set; } = [];
    public List<CourseDto> Completed { get; set; } = [];
}
