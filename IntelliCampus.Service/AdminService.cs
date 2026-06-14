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
            Roles = ResolveAdminRoles(dto.AdminRole),
            AdminCode = dto.AdminCode,
            HireDate = hireDate,
            FacultyId = facultyId
        };

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

        if (admin.Roles.Contains(UserRole.SuperAdmin))
            throw new InvalidOperationException("Cannot delete the SuperAdmin account.");

        Admins.Delete(admin);
        await _unitOfWork.SaveChangesAsync();

        return true;
    }

    private static List<UserRole> ResolveAdminRoles(string? adminRole)
    {
        if (string.IsNullOrWhiteSpace(adminRole))
            return [UserRole.Admin_UnderGrad];

        return adminRole.ToLowerInvariant() switch
        {
            "undergrad" or "under_grad" => [UserRole.Admin_UnderGrad],
            "postgrad" or "post_grad" => [UserRole.Admin_PostGrad],
            "academicstaff" or "academic_staff" => [UserRole.Admin_AcademicStaff],
            "superadmin" => [UserRole.SuperAdmin],
            _ => [UserRole.Admin_UnderGrad]
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
            Roles = admin.Roles.Select(r => r.ToString()).ToList()
        };
    }
}
