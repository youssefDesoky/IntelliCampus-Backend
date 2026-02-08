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

        if (dto.DepartmentId.HasValue)
        {
            var departmentExists = await _context.Departments.AnyAsync(d => d.DepartmentId == dto.DepartmentId);
            if (!departmentExists)
                throw new InvalidOperationException("Department not found.");
        }

        var instructor = new Instructor
        {
            NationalId = dto.NationalId,
            FullName = dto.FullName,
            FullNameAr = dto.FullNameAr,
            PhoneNumber = dto.PhoneNumber,
            Email = dto.Email,
            Address = dto.Address,
            Password = _passwordService.HashPassword(dto.Password),
            Nationality = dto.Nationality,
            Role = UserRole.Instructor,
            InstructorRole = dto.Role,
            Specialization = dto.Specialization,
            DepartmentId = dto.DepartmentId,
            HireDate = dto.HireDate ?? DateTime.UtcNow
        };

        _context.Instructors.Add(instructor);
        await _context.SaveChangesAsync();

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
        if (dto.Role is not null) instructor.InstructorRole = dto.Role;
        if (dto.Specialization is not null) instructor.Specialization = dto.Specialization;
        if (dto.DepartmentId.HasValue) instructor.DepartmentId = dto.DepartmentId;

        await _context.SaveChangesAsync();

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
            Role = instructor.InstructorRole,
            Specialization = instructor.Specialization,
            DepartmentId = instructor.DepartmentId,
            DepartmentName = instructor.Department?.DepartmentName,
            HireDate = instructor.HireDate
        };
    }
}
