using IntelliCampus.Shared.Dtos.Department;

namespace IntelliCampus.Service_Abstraction;

public interface IDepartmentService
{
    Task<DepartmentDto?> GetByIdAsync(int departmentId);
    Task<IEnumerable<DepartmentDto>> GetAllAsync();
    Task<DepartmentDto> CreateAsync(CreateDepartmentDto dto);
    Task<DepartmentDto?> UpdateAsync(int departmentId, UpdateDepartmentDto dto);
    Task<bool> DeleteAsync(int departmentId);
}
