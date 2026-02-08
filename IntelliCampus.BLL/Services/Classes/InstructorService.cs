using System.Globalization;
using IntelliCampus.BLL.Dtos.Instructor;
using IntelliCampus.BLL.Services.Interfaces;
using IntelliCampus.DAL.Data.Contexts;
using IntelliCampus.DAL.Entities;
using IntelliCampus.DAL.Entities.Enums;
using Microsoft.EntityFrameworkCore;

namespace IntelliCampus.BLL.Services.Classes;

public class InstructorService : IInstructorService
{
    private readonly IntelliCampusDbContext _context;
    private readonly IPasswordService _passwordService;

    public InstructorService(IntelliCampusDbContext context, IPasswordService passwordService)
    {
        _context = context;
        _passwordService = passwordService;
    }

    public async Task<InstructorDto?> GetByIdAsync(int instructorId)
    {
        var instructor = await _context.Instructors
            .Include(i => i.Department)
            .FirstOrDefaultAsync(i => i.UserId == instructorId);

        if (instructor is null)
            return null;

        return MapToDto(instructor);
    }

    public async Task<IEnumerable<InstructorDto>> GetAllAsync()
    {
        var instructors = await _context.Instructors
            .Include(i => i.Department)
            .ToListAsync();

        return instructors.Select(MapToDto);
    }

    public async Task<InstructorDto> CreateAsync(CreateInstructorDto dto)
    {
        if (await _context.Users.AnyAsync(u => u.Email == dto.Email))
            throw new InvalidOperationException("Email already exists.");

        if (await _context.Users.AnyAsync(u => u.NationalId == dto.NationalId))
            throw new InvalidOperationException("National ID already exists.");

        var departmentId = await ResolveDepartmentIdAsync(dto.DepartmentName);
        var hireDate = ParseDate(dto.HireDate) ?? DateTime.UtcNow;
        var password = string.IsNullOrWhiteSpace(dto.Password) ? "Instructor@123" : dto.Password;

        var instructor = new Instructor
        {
            NationalId = dto.NationalId,
            FullName = dto.FullName,
            FullNameAr = dto.FullNameAr,
            PhoneNumber = dto.PhoneNumber,
            Email = dto.Email,
            Address = dto.Address,
            Password = _passwordService.HashPassword(password),
            Nationality = dto.Nationality,
            Role = UserRole.Instructor,
            InstructorCode = dto.InstructorCode,
            InstructorRole = dto.Role,
            Specialization = dto.Specialization,
            DepartmentId = departmentId,
            HireDate = hireDate
        };

        _context.Instructors.Add(instructor);
        await _context.SaveChangesAsync();

        if (instructor.DepartmentId.HasValue)
            await _context.Entry(instructor).Reference(i => i.Department).LoadAsync();

        return MapToDto(instructor);
    }

    public async Task<InstructorDto?> UpdateAsync(int instructorId, UpdateInstructorDto dto)
    {
        var instructor = await _context.Instructors
            .Include(i => i.Department)
            .FirstOrDefaultAsync(i => i.UserId == instructorId);

        if (instructor is null)
            return null;

        if (dto.Email is not null && dto.Email != instructor.Email)
        {
            if (await _context.Users.AnyAsync(u => u.Email == dto.Email && u.UserId != instructorId))
                throw new InvalidOperationException("Email already exists.");
            instructor.Email = dto.Email;
        }

        if (dto.FullName is not null) instructor.FullName = dto.FullName;
        if (dto.FullNameAr is not null) instructor.FullNameAr = dto.FullNameAr;
        if (dto.PhoneNumber is not null) instructor.PhoneNumber = dto.PhoneNumber;
        if (dto.Address is not null) instructor.Address = dto.Address;
        if (dto.Nationality is not null) instructor.Nationality = dto.Nationality;
        if (dto.InstructorCode is not null) instructor.InstructorCode = dto.InstructorCode;
        if (dto.Role is not null) instructor.InstructorRole = dto.Role;
        if (dto.Specialization is not null) instructor.Specialization = dto.Specialization;

        var departmentId = await ResolveDepartmentIdAsync(dto.DepartmentName);
        if (departmentId.HasValue) instructor.DepartmentId = departmentId;

        var hireDate = ParseDate(dto.HireDate);
        if (hireDate.HasValue) instructor.HireDate = hireDate.Value;

        await _context.SaveChangesAsync();

        if (instructor.DepartmentId.HasValue)
            await _context.Entry(instructor).Reference(i => i.Department).LoadAsync();

        return MapToDto(instructor);
    }

    public async Task<bool> DeleteAsync(int instructorId)
    {
        var instructor = await _context.Instructors
            .FirstOrDefaultAsync(i => i.UserId == instructorId);

        if (instructor is null)
            return false;

        _context.Instructors.Remove(instructor);
        await _context.SaveChangesAsync();

        return true;
    }

    private async Task<int?> ResolveDepartmentIdAsync(string? departmentName)
    {
        if (string.IsNullOrWhiteSpace(departmentName))
            return null;

        if (int.TryParse(departmentName, out var id))
        {
            if (await _context.Departments.AnyAsync(d => d.DepartmentId == id))
                return id;
        }

        var department = await _context.Departments
            .FirstOrDefaultAsync(d => d.DepartmentName.ToLower() == departmentName.ToLower());

        if (department is not null)
            return department.DepartmentId;

        var normalized = departmentName.Trim();
        var departments = await _context.Departments.ToListAsync();
        var matched = departments.FirstOrDefault(d =>
            string.Equals(GetDepartmentCode(d.DepartmentName), normalized, StringComparison.OrdinalIgnoreCase));

        if (matched is null)
            throw new InvalidOperationException("Department not found.");

        return matched.DepartmentId;
    }

    private static string GetDepartmentCode(string departmentName)
    {
        var parts = departmentName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return string.Concat(parts.Select(p => char.ToUpperInvariant(p[0])));
    }

    private static DateTime? ParseDate(string? dateStr)
    {
        if (string.IsNullOrWhiteSpace(dateStr))
            return null;

        var formats = new[] { "M/d/yyyy", "d/M/yyyy", "M-d-yyyy", "d-M-yyyy", "MM/dd/yyyy", "dd/MM/yyyy", "yyyy-MM-dd" };

        if (DateTime.TryParseExact(dateStr, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
            return date;

        if (DateTime.TryParse(dateStr, CultureInfo.InvariantCulture, DateTimeStyles.None, out date))
            return date;

        throw new InvalidOperationException("Invalid date format.");
    }

    private static InstructorDto MapToDto(Instructor instructor)
    {
        return new InstructorDto
        {
            InstructorId = instructor.InstructorId,
            UserId = instructor.UserId,
            NationalId = instructor.NationalId,
            FullName = instructor.FullName,
            FullNameAr = instructor.FullNameAr,
            PhoneNumber = instructor.PhoneNumber,
            Email = instructor.Email,
            Address = instructor.Address,
            Nationality = instructor.Nationality,
            InstructorCode = instructor.InstructorCode,
            Role = instructor.InstructorRole,
            Specialization = instructor.Specialization,
            DepartmentId = instructor.DepartmentId,
            DepartmentName = instructor.Department?.DepartmentName,
            HireDate = instructor.HireDate
        };
    }
}
