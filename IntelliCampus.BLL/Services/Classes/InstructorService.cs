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
        // UserId is the PK in TPT inheritance
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
        // Check if email already exists
        if (await _context.Users.AnyAsync(u => u.Email == dto.Email))
            throw new InvalidOperationException("Email already exists.");

        // Check if national ID already exists
        if (await _context.Users.AnyAsync(u => u.NationalId == dto.NationalId))
            throw new InvalidOperationException("National ID already exists.");

        // Validate department if provided
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
            Role = UserRole.Instructor,
            InstructorRole = dto.Role,
            Specialization = dto.Specialization,
            DepartmentId = dto.DepartmentId
        };

        _context.Instructors.Add(instructor);
        await _context.SaveChangesAsync();

        // Reload with department
        await _context.Entry(instructor).Reference(i => i.Department).LoadAsync();

        return MapToDto(instructor);
    }

    public async Task<bool> DeleteAsync(int instructorId)
    {
        // UserId is the PK in TPT inheritance
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
            Role = instructor.InstructorRole,
            Specialization = instructor.Specialization,
            DepartmentId = instructor.DepartmentId,
            DepartmentName = instructor.Department?.DepartmentName
        };
    }
}
