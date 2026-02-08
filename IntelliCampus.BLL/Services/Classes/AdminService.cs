using System.Globalization;
using IntelliCampus.BLL.Dtos.Admin;
using IntelliCampus.BLL.Services.Interfaces;
using IntelliCampus.DAL.Data.Contexts;
using IntelliCampus.DAL.Entities;
using IntelliCampus.DAL.Entities.Enums;
using Microsoft.EntityFrameworkCore;

namespace IntelliCampus.BLL.Services.Classes;

public class AdminService : IAdminService
{
    private readonly IntelliCampusDbContext _context;
    private readonly IPasswordService _passwordService;

    public AdminService(IntelliCampusDbContext context, IPasswordService passwordService)
    {
        _context = context;
        _passwordService = passwordService;
    }

    public async Task<AdminDto?> GetByIdAsync(int adminId)
    {
        var admin = await _context.Admins
            .FirstOrDefaultAsync(a => a.UserId == adminId);

        if (admin is null)
            return null;

        return MapToDto(admin);
    }

    public async Task<IEnumerable<AdminDto>> GetAllAsync()
    {
        var admins = await _context.Admins.ToListAsync();
        return admins.Select(MapToDto);
    }

    public async Task<AdminDto> CreateAsync(CreateAdminDto dto)
    {
        if (await _context.Users.AnyAsync(u => u.Email == dto.Email))
            throw new InvalidOperationException("Email already exists.");

        if (await _context.Users.AnyAsync(u => u.NationalId == dto.NationalId))
            throw new InvalidOperationException("National ID already exists.");

        var hireDate = ParseDate(dto.HireDate) ?? DateTime.UtcNow;
        var password = string.IsNullOrWhiteSpace(dto.Password) ? "Admin@123" : dto.Password;

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
            Role = UserRole.Admin,
            AdminCode = dto.AdminCode,
            HireDate = hireDate
        };

        _context.Admins.Add(admin);
        await _context.SaveChangesAsync();

        return MapToDto(admin);
    }

    public async Task<bool> DeleteAsync(int adminId)
    {
        var admin = await _context.Admins
            .FirstOrDefaultAsync(a => a.UserId == adminId);

        if (admin is null)
            return false;

        if (admin.Role == UserRole.SuperAdmin)
            throw new InvalidOperationException("Cannot delete the SuperAdmin account.");

        _context.Admins.Remove(admin);
        await _context.SaveChangesAsync();

        return true;
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
            HireDate = admin.HireDate
        };
    }
}
