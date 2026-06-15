using IntelliCampus.Service.Resolvers;
using IntelliCampus.Shared.Dtos.Auth;
using IntelliCampus.Service_Abstraction;
using IntelliCampus.Domain.Interfaces;
using IntelliCampus.Domain.Entities;
using IntelliCampus.Service.Specifications;

namespace IntelliCampus.Service;

public class AuthService(
    IUnitOfWork unitOfWork,
    IPasswordService passwordService,
    ITokenService tokenService,
    INotificationService notificationService,
    UrlResolver urlResolver) : IAuthService
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly IPasswordService _passwordService = passwordService;
    private readonly ITokenService _tokenService = tokenService;
    private readonly UrlResolver _urlResolver = urlResolver;

    public async Task<AuthResponseDto?> LoginAsync(LoginDto dto)
    {
        var spec = new UserByEmailSpec(dto.Email);
        var user = await _unitOfWork.GetRepository<User, int>().GetByIdAsync(spec);

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
            Roles = user.UserRoles.Where(ur => ur.IsActive).Select(ur => ur.Role.RoleName).ToList(),
            Token = token,
            ExpiresAt = expiresAt
        };
    }

    private readonly INotificationService _notificationService = notificationService;

    public async Task<MeResponseDto?> GetMeAsync(int userId)
    {
        var spec = new UserByIdSpec(userId);
        var user = await _unitOfWork.GetRepository<User, int>().GetByIdAsync(spec);

        if (user is null)
            return null;

        return new MeResponseDto
        {
            UserId = user.UserId,
            FullName = user.FullName,
            Email = user.Email,
            Roles = user.UserRoles.Where(ur => ur.IsActive).Select(ur => ur.Role.RoleName).ToList(),
            ProfileImage = _urlResolver.ResolveProfile(user.ProfileImage),
            Notifications = (await _notificationService.GetUnreadAsync(userId)).ToList()
        };
    }

    public async Task<UserProfileDto?> GetProfileAsync(int userId)
    {
        var spec = new UserByIdSpec(userId);
        var user = await _unitOfWork.GetRepository<User, int>().GetByIdAsync(spec);

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
            Roles = user.UserRoles.Where(ur => ur.IsActive).Select(ur => ur.Role.RoleName).ToList(),
            ProfileImage = _urlResolver.ResolveProfile(user.ProfileImage)
        };
    }

    public async Task<UserProfileDto?> UpdateProfileAsync(int userId, UpdateProfileDto dto)
    {
        var spec = new UserByIdSpec(userId);
        var user = await _unitOfWork.GetRepository<User, int>().GetByIdAsync(spec);

        if (user is null)
            return null;

        if (dto.Address is not null)
            user.Address = dto.Address;

        if (dto.PhoneNumber is not null)
            user.PhoneNumber = dto.PhoneNumber;

        if (dto.FullNameAr is not null)
            user.FullNameAr = dto.FullNameAr;

        _unitOfWork.GetRepository<User, int>().Update(user);
        await _unitOfWork.SaveChangesAsync();

        return new UserProfileDto
        {
            UserId = user.UserId,
            NationalId = user.NationalId,
            FullName = user.FullName,
            FullNameAr = user.FullNameAr,
            PhoneNumber = user.PhoneNumber,
            Email = user.Email,
            Address = user.Address,
            Roles = user.UserRoles.Where(ur => ur.IsActive).Select(ur => ur.Role.RoleName).ToList(),
            ProfileImage = _urlResolver.ResolveProfile(user.ProfileImage)
        };
    }

    public async Task<string?> UpdateProfileImageAsync(int userId, string imageUrl)
    {
        var user = await _unitOfWork.GetRepository<User, int>().GetByIdAsync(userId);

        if (user is null)
            return null;

        user.ProfileImage = imageUrl;
        _unitOfWork.GetRepository<User, int>().Update(user);
        await _unitOfWork.SaveChangesAsync();

        return user.ProfileImage;
    }

    public async Task<bool> ChangePasswordAsync(int userId, ChangePasswordDto dto)
    {
        var user = await _unitOfWork.GetRepository<User, int>().GetByIdAsync(userId);

        if (user is null)
            return false;

        if (!_passwordService.VerifyPassword(dto.CurrentPassword, user.Password))
            return false;

        user.Password = _passwordService.HashPassword(dto.NewPassword);
        _unitOfWork.GetRepository<User, int>().Update(user);
        await _unitOfWork.SaveChangesAsync();

        return true;
    }
}