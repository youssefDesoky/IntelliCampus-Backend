using IntelliCampus.BLL.Dtos.Auth;

namespace IntelliCampus.BLL.Services.Interfaces;

public interface IAuthService
{
    Task<AuthResponseDto?> LoginAsync(LoginDto dto);
    Task<MeResponseDto?> GetMeAsync(int userId);
    Task<UserProfileDto?> GetProfileAsync(int userId);
    Task<UserProfileDto?> UpdateProfileAsync(int userId, UpdateProfileDto dto);
    Task<string?> UpdateProfileImageAsync(int userId, string imageUrl);
    Task<bool> ChangePasswordAsync(int userId, ChangePasswordDto dto);
}
