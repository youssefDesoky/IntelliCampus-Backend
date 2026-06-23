using IntelliCampus.Shared.Dtos.Auth;
using Microsoft.AspNetCore.Http;

namespace IntelliCampus.Service_Abstraction;

public interface IAuthService
{
    Task<AuthResponseDto?> LoginAsync(LoginDto dto);
    Task<MeResponseDto?> GetMeAsync(int userId);
    Task<UserProfileDto?> GetProfileAsync(int userId);
    Task<UserProfileDto?> UpdateProfileAsync(int userId, UpdateProfileDto dto);
    Task<UserProfileDto?> UpdateProfileImageAsync(int userId, IFormFile file);
    Task<bool> ChangePasswordAsync(int userId, ChangePasswordDto dto);
}
