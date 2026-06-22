using IntelliCampus.Shared.Dtos.Instructor;

namespace IntelliCampus.Service_Abstraction;

public interface IInstructorService
{
    Task<InstructorDto> GetByIdAsync(int instructorId);
    Task<IEnumerable<InstructorDto>> GetAllAsync();
    Task<IEnumerable<InstructorDto>> GetProfessorsAsync(int? departmentId = null, int? facultyId = null);
    Task<InstructorDto> CreateAsync(CreateInstructorDto dto, int? creatorUserId = null);
    Task<InstructorDto> UpdateAsync(int instructorId, UpdateInstructorDto dto);
    Task DeleteAsync(int instructorId);
}
