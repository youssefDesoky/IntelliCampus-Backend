using IntelliCampus.BLL.Dtos.Instructor;

namespace IntelliCampus.BLL.Services.Interfaces;

public interface IInstructorService
{
    Task<InstructorDto?> GetByIdAsync(int instructorId);
    Task<IEnumerable<InstructorDto>> GetAllAsync();
    Task<InstructorDto> CreateAsync(CreateInstructorDto dto);
    Task<bool> DeleteAsync(int instructorId);
}
