using IntelliCampus.Shared.Dtos.Course;

namespace IntelliCampus.Service_Abstraction;

public interface ICourseService
{
    Task<CourseDto?> GetByIdAsync(int courseId);
    Task<IEnumerable<CourseDto>> GetAllAsync();
    Task<IEnumerable<CourseDto>> GetActiveCoursesAsync();
    Task<IEnumerable<CourseDto>> GetCoursesByStudentIdAsync(int studentId);
    Task<IEnumerable<CourseDto>> GetCoursesByInstructorIdAsync(int instructorId);
    Task<CourseDto> CreateAsync(CreateCourseDto dto);
    Task<bool> ActivateAsync(int courseId);
    Task<bool> DeactivateAsync(int courseId);
    Task<bool> DeleteAsync(int courseId);
}
