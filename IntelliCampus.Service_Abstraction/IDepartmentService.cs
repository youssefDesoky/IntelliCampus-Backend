using IntelliCampus.shared.Pagination;
using IntelliCampus.Shared.Dtos.Department;
using IntelliCampus.Shared.Params;

namespace IntelliCampus.Service_Abstraction;

public interface IDepartmentService
{
    Task<DepartmentDto?> GetByIdAsync(int departmentId);
    Task<PaginatedResult<DepartmentDto>> GetAllAsync(DepartmentQueryParams queryParams);
    Task<DepartmentDto> CreateAsync(CreateDepartmentDto dto, int? creatorUserId = null);
    Task<DepartmentDto?> UpdateAsync(int departmentId, UpdateDepartmentDto dto);
    Task<bool> DeleteAsync(int departmentId);
    Task<DepartmentDto?> UpdateRegistrationSettingsAsync(int departmentId, DepartmentRegistrationSettingsDto dto);
    Task<IEnumerable<DepartmentDto>> UpdateAllRegistrationSettingsAsync(DepartmentRegistrationSettingsDto dto);
}
