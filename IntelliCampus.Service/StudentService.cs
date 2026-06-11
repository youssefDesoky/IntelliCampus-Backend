using System.Globalization;
using IntelliCampus.Shared.Dtos.Student;
using IntelliCampus.Service_Abstraction;
using IntelliCampus.Domain.Entities;
using IntelliCampus.Domain.Entities.Enums;
using IntelliCampus.Domain.Interfaces;
using IntelliCampus.Service.Specifications;

namespace IntelliCampus.Service;

public class StudentService : IStudentService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordService _passwordService;

    public StudentService(IUnitOfWork unitOfWork, IPasswordService passwordService)
    {
        _unitOfWork = unitOfWork;
        _passwordService = passwordService;
    }

    private IGenericRepository<Student, int> Students
        => _unitOfWork.GetRepository<Student, int>();

    private IGenericRepository<User, int> Users
        => _unitOfWork.GetRepository<User, int>();

    private IGenericRepository<Department, int> Departments
        => _unitOfWork.GetRepository<Department, int>();

    private IGenericRepository<Bylaw, int> Bylaws
        => _unitOfWork.GetRepository<Bylaw, int>();

    public async Task<StudentDto?> GetByIdAsync(int studentId)
    {
        var spec = new StudentSpec(studentId);
        var student = await Students.GetByIdAsync(spec);

        if (student is null)
            return null;

        return MapToDto(student);
    }

    public async Task<IEnumerable<StudentDto>> GetAllAsync()
    {
        var spec = new StudentSpec();
        var students = await Students.GetAllAsync(spec);

        return students.Select(MapToDto);
    }

    public async Task<StudentDto> CreateAsync(CreateStudentDto dto)
    {
        if (await Users.AnyAsync(u => u.Email == dto.Email))
            throw new InvalidOperationException("Email already exists.");

        if (await Users.AnyAsync(u => u.NationalId == dto.NationalId))
            throw new InvalidOperationException("National ID already exists.");

        var departmentId = await ResolveDepartmentIdAsync(dto.DepartmentId, dto.DepartmentName);
        var bylawId = await ResolveBylawIdAsync(dto.BylawId, dto.BylawName);
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
            BylawId = bylawId,
            EnrollmentDate = enrollmentDate
        };

        Students.Add(student);
        await _unitOfWork.SaveChangesAsync();

        if (student.DepartmentId.HasValue)
        {
            var spec = new StudentSpec(student.UserId);
            var result = await Students.GetByIdAsync(spec);
            return MapToDto(result!);
        }

        return MapToDto(student);
    }

    public async Task<StudentDto?> UpdateAsync(int studentId, UpdateStudentDto dto)
    {
        var spec = new StudentSpec(studentId);
        var student = await Students.GetByIdAsync(spec);

        if (student is null)
            return null;

        if (dto.Email is not null && dto.Email != student.Email)
        {
            if (await Users.AnyAsync(u => u.Email == dto.Email && u.UserId != studentId))
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

        if (dto.BylawId.HasValue) student.BylawId = dto.BylawId;

        var enrollmentDate = ParseEnrollmentDate(dto.EnrollmentDate);
        if (enrollmentDate.HasValue) student.EnrollmentDate = enrollmentDate.Value;

        Students.Update(student);
        await _unitOfWork.SaveChangesAsync();

        if (student.DepartmentId.HasValue)
        {
            var updatedSpec = new StudentSpec(student.UserId);
            var result = await Students.GetByIdAsync(updatedSpec);
            return MapToDto(result!);
        }

        return MapToDto(student);
    }

    public async Task<bool> DeleteAsync(int studentId)
    {
        var spec = new StudentSpec(studentId);
        var student = await Students.GetByIdAsync(spec);

        if (student is null)
            return false;

        Students.Delete(student);
        await _unitOfWork.SaveChangesAsync();

        return true;
    }

    private async Task<int?> ResolveBylawIdAsync(int? bylawId, string? bylawName)
    {
        if (bylawId.HasValue)
            return bylawId;

        if (string.IsNullOrWhiteSpace(bylawName))
            return null;

        var bylaws = await Bylaws.GetAllAsync();
        var matched = bylaws.FirstOrDefault(b =>
            string.Equals(b.Name, bylawName, StringComparison.OrdinalIgnoreCase));

        return matched?.BylawId;
    }

    private async Task<int?> ResolveDepartmentIdAsync(int? departmentId, string? departmentName)
    {
        if (departmentId.HasValue)
            return departmentId;

        if (string.IsNullOrWhiteSpace(departmentName))
            return null;

        var paramSpec = new DepartmentByNameSpec(departmentName);
        var department = (await Departments.GetAllAsync(paramSpec)).FirstOrDefault();

        if (department is not null)
            return department.DepartmentId;

        var normalized = departmentName.Trim();
        var departments = await Departments.GetAllAsync();
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
            BylawId = student.BylawId,
            BylawName = student.Bylaw?.Name,
            BylawVersion = student.Bylaw?.Version,
            EnrollmentDate = student.EnrollmentDate
        };
    }
}
