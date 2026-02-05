using IntelliCampus.BLL.Dtos.Auth;

namespace IntelliCampus.BLL.Services.Interfaces;

public interface IAuthService
{
    Task<AuthResponseDto?> LoginAsync(LoginDto dto);
    Task<UserProfileDto?> GetProfileAsync(int userId);
    Task<bool> ChangePasswordAsync(int userId, ChangePasswordDto dto);
}
