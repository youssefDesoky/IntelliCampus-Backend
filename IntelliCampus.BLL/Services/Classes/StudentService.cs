using System.Globalization;
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
        var student = await _context.Students
            .Include(s => s.Department)
            .FirstOrDefaultAsync(s => s.UserId == studentId);

        if (student is null)
            return null;

        return MapToDto(student);
    }

    public async Task<IEnumerable<StudentDto>> GetAllAsync()
    {
        var students = await _context.Students
            .Include(s => s.Department)
            .ToListAsync();

        return students.Select(MapToDto);
    }

    public async Task<StudentDto> CreateAsync(CreateStudentDto dto)
    {
        if (await _context.Users.AnyAsync(u => u.Email == dto.Email))
            throw new InvalidOperationException("Email already exists.");

        if (await _context.Users.AnyAsync(u => u.NationalId == dto.NationalId))
            throw new InvalidOperationException("National ID already exists.");

        var departmentId = await ResolveDepartmentIdAsync(dto.DepartmentId, dto.DepartmentName);
        var enrollmentDate = ParseEnrollmentDate(dto.EnrollmentDate) ?? DateTime.UtcNow;
        var password = string.IsNullOrWhiteSpace(dto.Password) ? "Student@123" : dto.Password;

        var student = new Student
        {
            NationalId = dto.NationalId,
            FullName = dto.FullName,
            FullNameAr = dto.FullNameAr,
            PhoneNumber = dto.PhoneNumber,
            Email = dto.Email,
            Address = dto.Address,
            Password = _passwordService.HashPassword(password),
            Nationality = dto.Nationality,
            Role = UserRole.Student,
            StudentCode = dto.StudentCode,
            Faculty = dto.Faculty,
            Level = dto.Level,
            DepartmentId = departmentId,
            EnrollmentDate = enrollmentDate
        };

        _context.Students.Add(student);
        await _context.SaveChangesAsync();

        if (student.DepartmentId.HasValue)
            await _context.Entry(student).Reference(s => s.Department).LoadAsync();

        return MapToDto(student);
    }

    public async Task<StudentDto?> UpdateAsync(int studentId, UpdateStudentDto dto)
    {
        var student = await _context.Students
            .Include(s => s.Department)
            .FirstOrDefaultAsync(s => s.UserId == studentId);

        if (student is null)
            return null;

        if (dto.Email is not null && dto.Email != student.Email)
        {
            if (await _context.Users.AnyAsync(u => u.Email == dto.Email && u.UserId != studentId))
                throw new InvalidOperationException("Email already exists.");
            student.Email = dto.Email;
        }

        if (dto.FullName is not null) student.FullName = dto.FullName;
        if (dto.FullNameAr is not null) student.FullNameAr = dto.FullNameAr;
        if (dto.PhoneNumber is not null) student.PhoneNumber = dto.PhoneNumber;
        if (dto.Address is not null) student.Address = dto.Address;
        if (dto.Nationality is not null) student.Nationality = dto.Nationality;
        if (dto.StudentCode is not null) student.StudentCode = dto.StudentCode;
        if (dto.Faculty is not null) student.Faculty = dto.Faculty;
        if (dto.Level.HasValue) student.Level = dto.Level;

        var departmentId = await ResolveDepartmentIdAsync(dto.DepartmentId, dto.DepartmentName);
        if (departmentId.HasValue) student.DepartmentId = departmentId;

        var enrollmentDate = ParseEnrollmentDate(dto.EnrollmentDate);
        if (enrollmentDate.HasValue) student.EnrollmentDate = enrollmentDate.Value;

        await _context.SaveChangesAsync();

        if (student.DepartmentId.HasValue)
            await _context.Entry(student).Reference(s => s.Department).LoadAsync();

        return MapToDto(student);
    }

    public async Task<bool> DeleteAsync(int studentId)
    {
        var student = await _context.Students
            .FirstOrDefaultAsync(s => s.UserId == studentId);

        if (student is null)
            return false;

        _context.Students.Remove(student);
        await _context.SaveChangesAsync();

        return true;
    }

    private async Task<int?> ResolveDepartmentIdAsync(int? departmentId, string? departmentName)
    {
        if (departmentId.HasValue)
            return departmentId;

        if (string.IsNullOrWhiteSpace(departmentName))
            return null;

        var department = await _context.Departments
            .FirstOrDefaultAsync(d => d.DepartmentName.ToLower() == departmentName.ToLower());

        if (department is not null)
            return department.DepartmentId;

        var normalized = departmentName.Trim();
        var departments = await _context.Departments.ToListAsync();
        var matched = departments.FirstOrDefault(d => string.Equals(GetDepartmentCode(d.DepartmentName), normalized, StringComparison.OrdinalIgnoreCase));

        if (matched is null)
            throw new InvalidOperationException("Department not found.");

        return matched.DepartmentId;
    }

    private static string GetDepartmentCode(string departmentName)
    {
        var parts = departmentName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return string.Concat(parts.Select(p => char.ToUpperInvariant(p[0])));
    }

    private static DateTime? ParseEnrollmentDate(string? enrollmentDate)
    {
        if (string.IsNullOrWhiteSpace(enrollmentDate))
            return null;

        var formats = new[] { "M/d/yyyy", "d/M/yyyy", "MM/dd/yyyy", "dd/MM/yyyy", "yyyy-MM-dd" };

        if (DateTime.TryParseExact(enrollmentDate, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
            return date;

        if (DateTime.TryParse(enrollmentDate, CultureInfo.InvariantCulture, DateTimeStyles.None, out date))
            return date;

        throw new InvalidOperationException("Invalid enrollment date format.");
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
            Nationality = student.Nationality,
            StudentCode = student.StudentCode,
            Faculty = student.Faculty,
            Level = student.Level,
            DepartmentId = student.DepartmentId,
            DepartmentName = student.Department?.DepartmentName,
            EnrollmentDate = student.EnrollmentDate
        };
    }
}
