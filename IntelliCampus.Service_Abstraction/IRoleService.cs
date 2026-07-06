using IntelliCampus.Shared.Dtos.Role;

namespace IntelliCampus.Service_Abstraction;

public interface IRoleService
{
    Task<IEnumerable<RoleDto>> GetAllRolesAsync();
    Task<IEnumerable<RoleDto>> GetAssignableRolesAsync();
    Task<IEnumerable<UserRoleDto>> GetUserRolesAsync(int userId);
    Task<UserRoleDto> AssignRoleAsync(AssignRoleDto dto);
    Task<UserRoleDto> UpdateUserRoleAsync(int userId, int roleId, UpdateUserRoleDto dto);
    Task<bool> RemoveRoleAsync(int userId, int roleId);
}
