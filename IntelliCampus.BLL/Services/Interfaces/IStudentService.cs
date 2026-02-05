using IntelliCampus.BLL.Dtos.Student;

namespace IntelliCampus.BLL.Services.Interfaces;

public interface IStudentService
{
    Task<StudentDto?> GetByIdAsync(int studentId);
    Task<IEnumerable<StudentDto>> GetAllAsync();
    Task<StudentDto> CreateAsync(CreateStudentDto dto);
    Task<bool> DeleteAsync(int studentId);
}
