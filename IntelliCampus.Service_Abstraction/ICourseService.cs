using IntelliCampus.Domain.Entities.Enums;
using IntelliCampus.shared.Pagination;
using IntelliCampus.Shared.Dtos.Bylaw;
using IntelliCampus.Shared.Dtos.Course;
using IntelliCampus.Shared.Dtos.Student;
using IntelliCampus.Shared.Params;
using Microsoft.AspNetCore.Http;

namespace IntelliCampus.Service_Abstraction;

public interface ICourseService
{
    Task<CourseDto?> GetByIdAsync(int courseId, int? studentId = null);
    Task<PaginatedResult<CourseDto>> GetAllAsync(CourseQueryParams queryParams);
    Task<PaginatedResult<CourseDto>> GetActiveCoursesAsync(CourseQueryParams queryParams);
    Task<PaginatedResult<CourseDto>> GetActiveCoursesByStudentBylawAsync(int studentId, CourseQueryParams queryParams);
    Task<PaginatedResult<CourseDto>> GetCoursesByStudentIdAsync(CourseQueryParams queryParams);
    Task<PaginatedResult<CourseDto>> GetCoursesByStudentBylawAsync(CourseQueryParams queryParams);
    Task<PaginatedResult<CourseDto>> GetCoursesByInstructorIdAsync(CourseQueryParams queryParams);
    Task<CourseDto> CreateAsync(CreateCourseDto dto);
    Task<CourseDto?> UpdateAsync(int courseId, CreateCourseDto dto);
    Task<bool> ActivateAsync(int courseId);
    Task<bool> DeactivateAsync(int courseId);
    Task<bool> DeleteAsync(int courseId);
    Task<IEnumerable<CoursePrerequisiteDto>?> GetPrerequisitesAsync(int courseId);
    Task<PaginatedResult<CoursePrerequisiteDto>> GetAllWithPrerequisitesAsync(CourseQueryParams queryParams);
    Task<PaginatedResult<CoursePrerequisiteDto>> GetAllWithPrerequisitesByStudentBylawAsync(int studentId, CourseQueryParams queryParams);
    Task<IEnumerable<StudentDto>> GetStudentsByCourseIdAsync(int courseId, string? search = null);
    Task<CourseDto> UpdateRegistrationSettingsAsync(int courseId, UpdateCourseRegistrationSettingsDto dto);
    Task<CourseRegistrationSettingsDto?> GetRegistrationSettingsAsync(int courseId);
    Task<ExcelImportResultDto> UploadGradesAsync(int courseId, IFormFile file, int? userId);
    Task<StudentAllCoursesDto> GetAllStudentCoursesAsync(int studentId);
    Task<CourseDto> ReactivateCourseAsync(int oldCourseId);
}
