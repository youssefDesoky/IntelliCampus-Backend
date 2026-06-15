using IntelliCampus.Shared.Dtos.Student;

namespace IntelliCampus.Service_Abstraction;

public interface IStudentService
{
    Task<StudentDto?> GetByIdAsync(int studentId);
    Task<IEnumerable<StudentDto>> GetAllAsync();
    Task<StudentDto> CreateAsync(CreateStudentDto dto, int? creatorUserId = null);
    Task<StudentDto?> UpdateAsync(int studentId, UpdateStudentDto dto);
    Task<StudentDto?> UpdateLevelAsync(int studentId, int level);
    Task<bool> DeleteAsync(int studentId);
}
