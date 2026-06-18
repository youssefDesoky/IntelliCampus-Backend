using IntelliCampus.Shared.Dtos.Admin;

namespace IntelliCampus.Service_Abstraction;

public interface IAdminService
{
    Task<AdminDto> GetByIdAsync(int adminId);
    Task<IEnumerable<AdminDto>> GetAllAsync();
    Task<AdminDto> CreateAsync(CreateAdminDto dto, int? creatorUserId = null);
    Task DeleteAsync(int adminId);
}
