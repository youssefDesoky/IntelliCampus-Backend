using IntelliCampus.BLL.Dtos.Student;
using IntelliCampus.BLL.Services.Interfaces;
using IntelliCampus.DAL.Data.Contexts;
using IntelliCampus.DAL.Entities;
using IntelliCampus.DAL.Entities.Enums;
using Microsoft.EntityFrameworkCore;

namespace IntelliCampus.BLL.Services.Classes;

public class StudentService : IStudentService
{
    private readonly IntelliCampusDbContext _context;
    private readonly IPasswordService _passwordService;

    public StudentService(IntelliCampusDbContext context, IPasswordService passwordService)
    {
        _context = context;
        _passwordService = passwordService;
    }

    public async Task<StudentDto?> GetByIdAsync(int studentId)
    {
        // UserId is the PK in TPT inheritance
        var student = await _context.Students
            .FirstOrDefaultAsync(s => s.UserId == studentId);

        if (student is null)
            return null;

        return MapToDto(student);
    }

    public async Task<IEnumerable<StudentDto>> GetAllAsync()
    {
        var students = await _context.Students
            .ToListAsync();

        return students.Select(MapToDto);
    }

    public async Task<StudentDto> CreateAsync(CreateStudentDto dto)
    {
        // Check if email already exists
        if (await _context.Users.AnyAsync(u => u.Email == dto.Email))
            throw new InvalidOperationException("Email already exists.");

        // Check if national ID already exists
        if (await _context.Users.AnyAsync(u => u.NationalId == dto.NationalId))
            throw new InvalidOperationException("National ID already exists.");

        var student = new Student
        {
            NationalId = dto.NationalId,
            FullName = dto.FullName,
            FullNameAr = dto.FullNameAr,
            PhoneNumber = dto.PhoneNumber,
            Email = dto.Email,
            Address = dto.Address,
            Password = _passwordService.HashPassword(dto.Password),
            Role = UserRole.Student,
            Faculty = dto.Faculty,
            Level = dto.Level
        };

        _context.Students.Add(student);
        await _context.SaveChangesAsync();

        return MapToDto(student);
    }

    public async Task<bool> DeleteAsync(int studentId)
    {
        // UserId is the PK in TPT inheritance
        var student = await _context.Students
            .FirstOrDefaultAsync(s => s.UserId == studentId);

        if (student is null)
            return false;

        _context.Students.Remove(student);
        await _context.SaveChangesAsync();

        return true;
    }

    private static StudentDto MapToDto(Student student)
    {
        return new StudentDto
        {
            StudentId = student.StudentId,
            UserId = student.UserId,
            NationalId = student.NationalId,
            FullName = student.FullName,
            FullNameAr = student.FullNameAr,
            PhoneNumber = student.PhoneNumber,
            Email = student.Email,
            Address = student.Address,
            Faculty = student.Faculty,
            Level = student.Level
        };
    }
}
