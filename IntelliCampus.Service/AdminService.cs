using System.Globalization;
using IntelliCampus.Shared.Dtos.Admin;
using IntelliCampus.Service_Abstraction;
using IntelliCampus.Domain.Entities;
using IntelliCampus.Domain.Entities.Enums;
using IntelliCampus.Domain.Interfaces;
using IntelliCampus.Service.Specifications;

namespace IntelliCampus.Service;

public class AdminService(IUnitOfWork unitOfWork, IPasswordService passwordService) : IAdminService
{
    private readonly IPasswordService _passwordService = passwordService;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private IGenericRepository<Admin, int> Admins
        => _unitOfWork.GetRepository<Admin, int>();

    private IGenericRepository<User, int> Users
        => _unitOfWork.GetRepository<User, int>();

    private IGenericRepository<Role, int> RolesRepo
        => _unitOfWork.GetRepository<Role, int>();

    public async Task<AdminDto?> GetByIdAsync(int adminId)
    {
        var spec = new AdminByIdSpec(adminId);
        var admin = await Admins.GetByIdAsync(spec);

        if (admin is null)
            return null;

        return MapToDto(admin);
    }

    public async Task<IEnumerable<AdminDto>> GetAllAsync()
    {
        var admins = await Admins.GetAllAsync();
        return admins.Select(MapToDto);
    }

    public async Task<AdminDto> CreateAsync(CreateAdminDto dto, int? creatorUserId = null)
    {
        if (await Users.AnyAsync(u => u.Email == dto.Email))
            throw new InvalidOperationException("Email already exists.");

        if (await Users.AnyAsync(u => u.NationalId == dto.NationalId))
            throw new InvalidOperationException("National ID already exists.");

        var hireDate = ParseDate(dto.HireDate) ?? DateTime.UtcNow;
        var password = string.IsNullOrWhiteSpace(dto.Password) ? "Admin@123" : dto.Password;

        var facultyId = dto.FacultyId;
        if (facultyId is null && creatorUserId.HasValue)
        {
            var creator = await Users.GetByIdAsync(creatorUserId.Value);
            facultyId = creator?.FacultyId;
        }

        var roleName = ResolveAdminRoleName(dto.AdminRole);
        var role = (await RolesRepo.GetAllAsync()).First(r => r.RoleName == roleName);

        var admin = new Admin
        {
            NationalId = dto.NationalId,
            FullName = dto.FullName,
            FullNameAr = dto.FullNameAr,
            PhoneNumber = dto.PhoneNumber,
            Email = dto.Email,
            Address = dto.Address,
            Password = _passwordService.HashPassword(password),
            Nationality = dto.Nationality,
            AdminCode = dto.AdminCode,
            HireDate = hireDate,
            FacultyId = facultyId
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

    public async Task<bool> DeleteAsync(int adminId)
    {
        var spec = new AdminByIdSpec(adminId);
        var admin = await Admins.GetByIdAsync(spec);

        if (admin is null)
            return false;

        if (admin.UserRoles.Any(ur => ur.IsActive && ur.Role.RoleName == nameof(UserRole.SuperAdmin)))
            throw new InvalidOperationException("Cannot delete the SuperAdmin account.");

        Admins.Delete(admin);
        await _unitOfWork.SaveChangesAsync();

        return true;
    }

    private static string ResolveAdminRoleName(string? adminRole)
    {
        return (adminRole?.ToLowerInvariant()) switch
        {
            "undergrad" or "under_grad" => "Admin_UnderGrad",
            "masters" or "postgrad" or "post_grad" => "Admin_Masters",
            "phd" => "Admin_PhD",
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

    private static AdminDto MapToDto(Admin admin)
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
            HireDate = admin.HireDate,
            FacultyId = admin.FacultyId,
            FacultyName = admin.Faculty?.FacultyName,
            Roles = admin.UserRoles.Where(ur => ur.IsActive).Select(ur => ur.Role.RoleName).ToList()
        };
    }
}
