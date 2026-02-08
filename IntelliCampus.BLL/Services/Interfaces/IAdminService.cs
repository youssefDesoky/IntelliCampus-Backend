using IntelliCampus.BLL.Dtos.Admin;

namespace IntelliCampus.BLL.Services.Interfaces;

public interface IAdminService
{
    Task<AdminDto?> GetByIdAsync(int adminId);
    Task<IEnumerable<AdminDto>> GetAllAsync();
    Task<AdminDto> CreateAsync(CreateAdminDto dto);
    Task<bool> DeleteAsync(int adminId);
}
