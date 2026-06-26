using IntelliCampus.shared.Pagination;
using IntelliCampus.Shared.Dtos.Admin;
using IntelliCampus.Shared.Params;

namespace IntelliCampus.Service_Abstraction;

public interface IAdminService
{
    Task<AdminDto> GetByIdAsync(int adminId);
    Task<PaginatedResult<AdminDto>> GetAllAsync(AdminQueryParams queryParams);
    Task<AdminDto> CreateAsync(CreateAdminDto dto, int? creatorUserId = null);
    Task<AdminDto> UpdateAsync(int adminId, UpdateAdminDto dto);
    Task DeleteAsync(int adminId);
}
