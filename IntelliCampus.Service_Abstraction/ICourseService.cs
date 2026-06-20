using IntelliCampus.Domain.Entities.Enums;
using IntelliCampus.Shared.Dtos.Course;
using IntelliCampus.Shared.Dtos.Student;

namespace IntelliCampus.Service_Abstraction;

public interface ICourseService
{
    Task<CourseDto?> GetByIdAsync(int courseId);
    Task<IEnumerable<CourseDto>> GetAllAsync();
    Task<IEnumerable<CourseDto>> GetActiveCoursesAsync();
    Task<IEnumerable<CourseDto>> GetCoursesByStudentIdAsync(int studentId, StudentCourseStatus? status = null);
    Task<IEnumerable<CourseDto>> GetCoursesByInstructorIdAsync(int instructorId);
    Task<CourseDto> CreateAsync(CreateCourseDto dto);
    Task<CourseDto?> UpdateAsync(int courseId, CreateCourseDto dto);
    Task<bool> ActivateAsync(int courseId);
    Task<bool> DeactivateAsync(int courseId);
    Task<bool> DeleteAsync(int courseId);
    Task<IEnumerable<CoursePrerequisiteDto>?> GetPrerequisitesAsync(int courseId);
    Task<IEnumerable<StudentDto>> GetStudentsByCourseIdAsync(int courseId);
}
