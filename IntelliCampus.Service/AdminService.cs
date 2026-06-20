using System.Globalization;
using IntelliCampus.Shared.Dtos.Admin;
using IntelliCampus.Service_Abstraction;
using IntelliCampus.Domain.Entities;
using IntelliCampus.Domain.Entities.Enums;
using IntelliCampus.Domain.Interfaces;
using IntelliCampus.Service.Specifications;
using IntelliCampus.Service.Exceptions;
using IntelliCampus.Service.Resolvers;

namespace IntelliCampus.Service;

public class AdminService(IUnitOfWork unitOfWork, IPasswordService passwordService, ICodeGenerationService codeGeneration, UrlResolver urlResolver) : IAdminService
{
    private readonly IPasswordService _passwordService = passwordService;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly ICodeGenerationService _codeGeneration = codeGeneration;
    private readonly UrlResolver _urlResolver = urlResolver;
    private IGenericRepository<Admin, int> Admins
        => _unitOfWork.GetRepository<Admin, int>();

    private IGenericRepository<User, int> Users
        => _unitOfWork.GetRepository<User, int>();

    private IGenericRepository<Role, int> RolesRepo
        => _unitOfWork.GetRepository<Role, int>();

    private IGenericRepository<Faculty, int> Faculties
        => _unitOfWork.GetRepository<Faculty, int>();

    public async Task<AdminDto> GetByIdAsync(int adminId)
    {
        var spec = new AdminSpec(adminId);
        var admin = await Admins.GetByIdAsync(spec);

        if (admin is null)
            throw new AdminNotFoundException(adminId);

        return MapToDto(admin);
    }

    public async Task<IEnumerable<AdminDto>> GetAllAsync()
    {
        var spec = new AdminSpec();
        var admins = await Admins.GetAllAsync(spec);
        return admins.Select(MapToDto);
    }

    public async Task<AdminDto> CreateAsync(CreateAdminDto dto, int? creatorUserId = null)
    {
        if (await Users.AnyAsync(u => u.NationalId == dto.NationalId))
            throw new InvalidOperationException("National ID already exists.");

        var hireDate = ParseDate(dto.HireDate) ?? DateTime.UtcNow;
        var password = string.IsNullOrWhiteSpace(dto.Password) ? dto.NationalId : dto.Password;

        var facultyId = dto.FacultyId;
        if (facultyId is null && creatorUserId.HasValue)
        {
            var creator = await Users.GetByIdAsync(creatorUserId.Value);
            facultyId = creator?.FacultyId;
        }

        if (facultyId.HasValue)
        {
            var faculty = await Faculties.GetByIdAsync(facultyId.Value);
            if (faculty is null)
                throw new InvalidOperationException($"Faculty with ID {facultyId.Value} not found.");
        }

        var code = dto.AdminCode;
        var email = dto.Email;

        if (string.IsNullOrWhiteSpace(code) && facultyId.HasValue)
            code = await _codeGeneration.GenerateAdminCodeAsync(facultyId.Value, hireDate);

        if (string.IsNullOrWhiteSpace(email))
            email = !string.IsNullOrWhiteSpace(code) ? code + "@intellicampus.online" : dto.Email;

        if (string.IsNullOrWhiteSpace(email))
            throw new InvalidOperationException("Email is required. Provide an email or ensure a faculty is assigned for auto-generation.");

        if (await Users.AnyAsync(u => u.Email == email))
            throw new InvalidOperationException("Email already exists.");

        var roleName = ResolveAdminRoleName(dto.AdminRole);
        var role = (await RolesRepo.GetAllAsync()).First(r => r.RoleName == roleName);

        var admin = new Admin
        {
            NationalId = dto.NationalId,
            FullName = dto.FullName,
            FullNameAr = dto.FullNameAr,
            PhoneNumber = dto.PhoneNumber,
            Email = email,
            Address = dto.Address,
            Password = _passwordService.HashPassword(password),
            Nationality = dto.Nationality,
            AdminCode = code,
            HireDate = hireDate,
            FacultyId = facultyId,
            ProfileImage = dto.ProfileImage
        };

        admin.UserRoles =
        [
            new UserRoleJunction
            {
                Role = role,
                IsActive = true,
                AssignedAt = DateTime.UtcNow
            }
        ];

        Admins.Add(admin);
        await _unitOfWork.SaveChangesAsync();

        return MapToDto(admin);
    }

    public async Task<AdminDto> UpdateAsync(int adminId, UpdateAdminDto dto)
    {
        var spec = new AdminSpec(adminId);
        var admin = await Admins.GetByIdAsync(spec);

        if (admin is null)
            throw new AdminNotFoundException(adminId);

        if (dto.Email is not null && dto.Email != admin.Email)
        {
            if (await Users.AnyAsync(u => u.Email == dto.Email && u.UserId != adminId))
                throw new InvalidOperationException("Email already exists.");
            admin.Email = dto.Email;
        }

        if (dto.FullName is not null) admin.FullName = dto.FullName;
        if (dto.FullNameAr is not null) admin.FullNameAr = dto.FullNameAr;
        if (dto.PhoneNumber is not null) admin.PhoneNumber = dto.PhoneNumber;
        if (dto.Address is not null) admin.Address = dto.Address;
        if (dto.Nationality is not null) admin.Nationality = dto.Nationality;
        if (dto.AdminCode is not null) admin.AdminCode = dto.AdminCode;
        if (dto.FacultyId.HasValue) admin.FacultyId = dto.FacultyId;
        if (dto.ProfileImage is not null) admin.ProfileImage = dto.ProfileImage;

        var hireDate = ParseDate(dto.HireDate);
        if (hireDate.HasValue) admin.HireDate = hireDate.Value;

        if (dto.AdminRole is not null)
        {
            var roleName = ResolveAdminRoleName(dto.AdminRole);
            var newRole = (await RolesRepo.GetAllAsync()).First(r => r.RoleName == roleName);
            var activeRole = admin.UserRoles.FirstOrDefault(ur => ur.IsActive);
            if (activeRole is not null)
            {
                activeRole.IsActive = false;
                activeRole.AssignedAt = DateTime.UtcNow;
            }
            admin.UserRoles.Add(new UserRoleJunction
            {
                Role = newRole,
                IsActive = true,
                AssignedAt = DateTime.UtcNow
            });
        }

        await _unitOfWork.SaveChangesAsync();
        return MapToDto(admin);
    }

    public async Task DeleteAsync(int adminId)
    {
        var spec = new AdminSpec(adminId);
        var admin = await Admins.GetByIdAsync(spec);

        if (admin is null)
            throw new AdminNotFoundException(adminId);

        if (admin.UserRoles.Any(ur => ur.IsActive && ur.Role.RoleName == nameof(UserRole.SuperAdmin)))
            throw new InvalidOperationException("Cannot delete the SuperAdmin account.");

        Admins.Delete(admin);
        await _unitOfWork.SaveChangesAsync();
    }

    private static string ResolveAdminRoleName(string? adminRole)
    {
        return (adminRole?.ToLowerInvariant()) switch
        {
            "undergrad" or "under_grad" => "Admin_UnderGrad",
            "masters" or "postgrad" or "post_grad" => "Admin_Masters",
            "phd" => "Admin_PhD",
            "diploma" => "Admin_Diploma",
            "academicstaff" or "academic_staff" => "Admin_AcademicStaff",
            "superadmin" => "SuperAdmin",
            _ => "Admin_UnderGrad"
        };
    }

    private static DateTime? ParseDate(string? dateStr)
    {
        if (string.IsNullOrWhiteSpace(dateStr))
            return null;

        var formats = new[] { "yyyy-MM-dd", "M/d/yyyy", "d/M/yyyy", "M-d-yyyy", "d-M-yyyy", "MM/dd/yyyy", "dd/MM/yyyy" };

        if (DateTime.TryParseExact(dateStr, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
            return date;

        if (DateTime.TryParse(dateStr, CultureInfo.InvariantCulture, DateTimeStyles.None, out date))
            return date;

        throw new InvalidOperationException("Invalid date format.");
    }

    private AdminDto MapToDto(Admin admin)
    {
        return new AdminDto
        {
            AdminId = admin.AdminId,
            UserId = admin.UserId,
            NationalId = admin.NationalId,
            FullName = admin.FullName,
            FullNameAr = admin.FullNameAr,
            PhoneNumber = admin.PhoneNumber,
            Email = admin.Email,
            Address = admin.Address,
            Nationality = admin.Nationality,
            AdminCode = admin.AdminCode,
            HireDate = admin.HireDate?.ToString("dd MM yyyy"),
            FacultyId = admin.FacultyId,
            FacultyName = admin.Faculty?.FacultyName,
            ProfileImage = _urlResolver.ResolveProfile(admin.ProfileImage),
            Roles = admin.UserRoles.Where(ur => ur.IsActive).Select(ur => ur.Role.RoleName).ToList()
        };
    }
}
