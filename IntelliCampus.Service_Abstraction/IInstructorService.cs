using IntelliCampus.Shared.Dtos.Instructor;
using IntelliCampus.Shared.Params;

namespace IntelliCampus.Service_Abstraction;

public interface IInstructorService
{
    Task<InstructorDto> GetByIdAsync(int instructorId);
    Task<IEnumerable<InstructorDto>> GetAllAsync();
    Task<IEnumerable<InstructorDto>> GetProfessorsAsync(InstructorQueryParams queryParams);
    Task<InstructorDto> CreateAsync(CreateInstructorDto dto, int? creatorUserId = null);
    Task<InstructorDto> UpdateAsync(int instructorId, UpdateInstructorDto dto);
    Task DeleteAsync(int instructorId);
}
