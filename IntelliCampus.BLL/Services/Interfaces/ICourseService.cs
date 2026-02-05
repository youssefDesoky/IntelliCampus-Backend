using IntelliCampus.BLL.Dtos.Course;

namespace IntelliCampus.BLL.Services.Interfaces;

public interface ICourseService
{
    Task<CourseDto?> GetByIdAsync(int courseId);
    Task<IEnumerable<CourseDto>> GetAllAsync();
    Task<IEnumerable<CourseDto>> GetActiveCoursesAsync();
    Task<CourseDto> CreateAsync(CreateCourseDto dto);
    Task<bool> ActivateAsync(int courseId);
    Task<bool> DeactivateAsync(int courseId);
    Task<bool> DeleteAsync(int courseId);
}
