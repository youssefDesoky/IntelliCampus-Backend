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

        var admin = new Admin
        {
            NationalId = dto.NationalId,
            FullName = dto.FullName,
            FullNameAr = dto.FullNameAr,
            PhoneNumber = dto.PhoneNumber,
            Email = dto.Email,
            Address = dto.Address,
            Password = _passwordService.HashPassword(dto.Password),
            Nationality = dto.Nationality,
            Role = UserRole.Admin,
            HireDate = dto.HireDate ?? DateTime.UtcNow
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
            HireDate = admin.HireDate
        };
    }
}
