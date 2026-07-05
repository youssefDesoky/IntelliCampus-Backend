using IntelliCampus.Shared.Dtos.Specialization;

namespace IntelliCampus.Service_Abstraction;

public interface ISpecializationService
{
    Task<IEnumerable<SpecializationDto>> GetAllAsync(string? search = null);
    Task<IEnumerable<SpecializationDto>> GetByDepartmentAsync(int departmentId);
    Task<SpecializationDto> GetByIdAsync(int id);
    Task<SpecializationDto> CreateAsync(CreateSpecializationDto dto);
    Task<SpecializationDto> UpdateAsync(int id, UpdateSpecializationDto dto);
    Task DeleteAsync(int id);
    Task<IEnumerable<SpecializationPrerequisiteDto>> GetPrerequisitesAsync(int specializationId);
    Task SetPrerequisitesAsync(int specializationId, SetSpecializationPrerequisitesDto dto);
}
