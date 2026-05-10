using IntelliCampus.Shared.Dtos.Instructor;

namespace IntelliCampus.Service_Abstraction;

public interface IInstructorService
{
    Task<InstructorDto?> GetByIdAsync(int instructorId);
    Task<IEnumerable<InstructorDto>> GetAllAsync();
    Task<InstructorDto> CreateAsync(CreateInstructorDto dto);
    Task<InstructorDto?> UpdateAsync(int instructorId, UpdateInstructorDto dto);
    Task<bool> DeleteAsync(int instructorId);
}
