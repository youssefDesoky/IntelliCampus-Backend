using IntelliCampus.Service.Exceptions;
using IntelliCampus.Service.Resolvers;
using IntelliCampus.Shared.Dtos.Auth;
using IntelliCampus.Shared.Params;
using IntelliCampus.Service_Abstraction;
using IntelliCampus.Domain.Interfaces;
using IntelliCampus.Domain.Entities;
using IntelliCampus.Service.Specifications;
using Microsoft.AspNetCore.Http;

namespace IntelliCampus.Service;

public class AuthService(
    IUnitOfWork unitOfWork,
    IPasswordService passwordService,
    ITokenService tokenService,
    INotificationService notificationService,
    IFileStorageService fileStorageService,
    UrlResolver urlResolver) : IAuthService
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly IPasswordService _passwordService = passwordService;
    private readonly ITokenService _tokenService = tokenService;
    private readonly IFileStorageService _fileStorageService = fileStorageService;
    private readonly UrlResolver _urlResolver = urlResolver;

    public async Task<AuthResponseDto?> LoginAsync(LoginDto dto)
    {
        var spec = new UserByEmailSpec(dto.Email);
        var user = await _unitOfWork.GetRepository<User, int>().GetByIdAsync(spec);

        if (user is null)
            throw new UnauthorizedAccessException("Invalid email or password.");

        if (!_passwordService.VerifyPassword(dto.Password, user.Password))
            throw new UnauthorizedAccessException("Invalid email or password.");

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
            throw new UserNotFoundException(userId);

        return new MeResponseDto
        {
            UserId = user.UserId,
            FullName = user.FullName,
            Email = user.Email,
            Roles = user.UserRoles.Where(ur => ur.IsActive).Select(ur => ur.Role.RoleName).ToList(),
            ProfileImage = _urlResolver.ResolveProfile(user.ProfileImage),
            MustChangePassword = user.MustChangePassword,
            Notifications = (await _notificationService.GetUnreadAsync(userId, new NotificationQueryParams())).ToList()
        };
    }

    public async Task<UserProfileDto?> GetProfileAsync(int userId)
    {
        var spec = new UserByIdSpec(userId);
        var user = await _unitOfWork.GetRepository<User, int>().GetByIdAsync(spec);

        if (user is null)
            throw new UserNotFoundException(userId);

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
            throw new UserNotFoundException(userId);

        if (dto.FullName is not null)
            user.FullName = dto.FullName;

        if (dto.Address is not null)
            user.Address = dto.Address;

        if (dto.PhoneNumber is not null)
            user.PhoneNumber = dto.PhoneNumber;

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

    public async Task<UserProfileDto?> UpdateProfileImageAsync(int userId, IFormFile file)
    {
        var spec = new UserByIdSpec(userId);
        var user = await _unitOfWork.GetRepository<User, int>().GetByIdAsync(spec);

        if (user is null)
            throw new UserNotFoundException(userId);

        var path = await _fileStorageService.SaveAsync(file, "profiles");
        user.ProfileImage = path;

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

    public async Task<bool> ChangePasswordAsync(int userId, ChangePasswordDto dto)
    {
        var user = await _unitOfWork.GetRepository<User, int>().GetByIdAsync(userId);

        if (user is null)
            throw new UserNotFoundException(userId);

        if (!_passwordService.VerifyPassword(dto.CurrentPassword, user.Password))
            throw new InvalidOperationException("Current password is incorrect.");

        user.Password = _passwordService.HashPassword(dto.NewPassword);
        _unitOfWork.GetRepository<User, int>().Update(user);
        await _unitOfWork.SaveChangesAsync();

        return true;
    }

    public async Task<bool> ChangeRecoveryEmailAsync(int userId, ChangeRecoveryEmailDto dto)
    {
        var user = await _unitOfWork.GetRepository<User, int>().GetByIdAsync(userId);

        if (user is null)
            throw new UserNotFoundException(userId);

        if (!_passwordService.VerifyPassword(dto.CurrentPassword, user.Password))
            throw new InvalidOperationException("Current password is incorrect.");

        user.RecoveryEmail = dto.NewEmail;
        user.RecoveryEmailVerified = true;
        _unitOfWork.GetRepository<User, int>().Update(user);
        await _unitOfWork.SaveChangesAsync();

        return true;
    }
}