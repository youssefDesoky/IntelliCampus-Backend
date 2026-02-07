using IntelliCampus.BLL.Dtos.Auth;
using IntelliCampus.BLL.Services.Interfaces;
using IntelliCampus.DAL.Data.Contexts;
using Microsoft.EntityFrameworkCore;

namespace IntelliCampus.BLL.Services.Classes;

public class AuthService : IAuthService
{
    private const string DefaultProfileImage = "/images/default-avatar.png";

    private readonly IntelliCampusDbContext _context;
    private readonly IPasswordService _passwordService;
    private readonly ITokenService _tokenService;

    public AuthService(
        IntelliCampusDbContext context,
        IPasswordService passwordService,
        ITokenService tokenService)
    {
        _context = context;
        _passwordService = passwordService;
        _tokenService = tokenService;
    }

    public async Task<AuthResponseDto?> LoginAsync(LoginDto dto)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == dto.Email);

        if (user is null)
            return null;

        if (!_passwordService.VerifyPassword(dto.Password, user.Password))
            return null;

        var (token, expiresAt) = _tokenService.GenerateToken(user);

        return new AuthResponseDto
        {
            UserId = user.UserId,
            Email = user.Email,
            FullName = user.FullName,
            Role = user.Role.ToString(),
            Token = token,
            ExpiresAt = expiresAt
        };
    }

    public async Task<MeResponseDto?> GetMeAsync(int userId)
    {
        var user = await _context.Users.FindAsync(userId);

        if (user is null)
            return null;

        return new MeResponseDto
        {
            UserId = user.UserId,
            FullName = user.FullName,
            Email = user.Email,
            Role = user.Role.ToString(),
            ProfileImage = user.ProfileImage ?? DefaultProfileImage,
            Notifications = new { message = "Notifications placeholder" }
        };
    }

    public async Task<UserProfileDto?> GetProfileAsync(int userId)
    {
        var user = await _context.Users.FindAsync(userId);

        if (user is null)
            return null;

        return new UserProfileDto
        {
            UserId = user.UserId,
            NationalId = user.NationalId,
            FullName = user.FullName,
            FullNameAr = user.FullNameAr,
            PhoneNumber = user.PhoneNumber,
            Email = user.Email,
            Address = user.Address,
            Role = user.Role.ToString(),
            ProfileImage = user.ProfileImage ?? DefaultProfileImage
        };
    }

    public async Task<UserProfileDto?> UpdateProfileAsync(int userId, UpdateProfileDto dto)
    {
        var user = await _context.Users.FindAsync(userId);

        if (user is null)
            return null;

        if (dto.Address is not null)
            user.Address = dto.Address;

        if (dto.PhoneNumber is not null)
            user.PhoneNumber = dto.PhoneNumber;

        if (dto.FullNameAr is not null)
            user.FullNameAr = dto.FullNameAr;

        await _context.SaveChangesAsync();

        return new UserProfileDto
        {
            UserId = user.UserId,
            NationalId = user.NationalId,
            FullName = user.FullName,
            FullNameAr = user.FullNameAr,
            PhoneNumber = user.PhoneNumber,
            Email = user.Email,
            Address = user.Address,
            Role = user.Role.ToString(),
            ProfileImage = user.ProfileImage ?? DefaultProfileImage
        };
    }

    public async Task<string?> UpdateProfileImageAsync(int userId, string imageUrl)
    {
        var user = await _context.Users.FindAsync(userId);

        if (user is null)
            return null;

        user.ProfileImage = imageUrl;
        await _context.SaveChangesAsync();

        return user.ProfileImage;
    }

    public async Task<bool> ChangePasswordAsync(int userId, ChangePasswordDto dto)
    {
        var user = await _context.Users.FindAsync(userId);

        if (user is null)
            return false;

        if (!_passwordService.VerifyPassword(dto.CurrentPassword, user.Password))
            return false;

        user.Password = _passwordService.HashPassword(dto.NewPassword);
        await _context.SaveChangesAsync();

        return true;
    }
}
