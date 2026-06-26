using IntelliCampus.shared.Pagination;
using IntelliCampus.Shared.Dtos.Student;
using IntelliCampus.Shared.Params;

namespace IntelliCampus.Service_Abstraction;

public interface IStudentService
{
    Task<StudentDto> GetByIdAsync(int studentId);
    Task<PaginatedResult<StudentDto>> GetAllAsync(StudentQueryParams queryParams);
    Task<StudentDto> CreateAsync(CreateStudentDto dto, int? creatorUserId = null);
    Task<StudentDto> UpdateAsync(int studentId, UpdateStudentDto dto);
    Task<StudentDto> UpdateLevelAsync(int studentId, int level);
    Task DeleteAsync(int studentId);
}
