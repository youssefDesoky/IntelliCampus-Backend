using System.Globalization;
using IntelliCampus.Shared.Dtos.Student;
using IntelliCampus.Service_Abstraction;
using IntelliCampus.Domain.Entities;
using IntelliCampus.Domain.Entities.Enums;
using IntelliCampus.Domain.Interfaces;
using IntelliCampus.Service.Specifications;
using IntelliCampus.Service.Exceptions;

namespace IntelliCampus.Service;

public class StudentService : IStudentService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordService _passwordService;
    private readonly ICodeGenerationService _codeGeneration;

    public StudentService(IUnitOfWork unitOfWork, IPasswordService passwordService, ICodeGenerationService codeGeneration)
    {
        _unitOfWork = unitOfWork;
        _passwordService = passwordService;
        _codeGeneration = codeGeneration;
    }

    private IGenericRepository<Student, int> Students
        => _unitOfWork.GetRepository<Student, int>();

    private IGenericRepository<User, int> Users
        => _unitOfWork.GetRepository<User, int>();

    private IGenericRepository<Department, int> Departments
        => _unitOfWork.GetRepository<Department, int>();

    private IGenericRepository<Bylaw, int> Bylaws
        => _unitOfWork.GetRepository<Bylaw, int>();

    private IGenericRepository<Role, int> RolesRepo
        => _unitOfWork.GetRepository<Role, int>();

    private IGenericRepository<Faculty, int> Faculties
        => _unitOfWork.GetRepository<Faculty, int>();

    private IGenericRepository<Specialization, int> Specializations
        => _unitOfWork.GetRepository<Specialization, int>();

    public async Task<StudentDto> GetByIdAsync(int studentId)
    {
        var spec = new StudentSpec(studentId);
        var student = await Students.GetByIdAsync(spec);

        if (student is null)
            throw new StudentNotFoundException(studentId);

        return MapToDto(student);
    }

    public async Task<IEnumerable<StudentDto>> GetAllAsync()
    {
        var spec = new StudentSpec();
        var students = await Students.GetAllAsync(spec);

        return students.Select(MapToDto);
    }

    public async Task<StudentDto> CreateAsync(CreateStudentDto dto, int? creatorUserId = null)
    {
        if (await Users.AnyAsync(u => u.NationalId == dto.NationalId))
            throw new InvalidOperationException("National ID already exists.");

        var departmentId = await ResolveDepartmentIdAsync(dto.DepartmentId, dto.DepartmentName);
        var bylawId = await ResolveBylawIdAsync(dto.BylawId, dto.BylawName);
        var enrollmentDate = ParseEnrollmentDate(dto.EnrollmentDate) ?? DateTime.UtcNow;
        var password = string.IsNullOrWhiteSpace(dto.Password) ? dto.NationalId : dto.Password;

        var facultyId = dto.FacultyId;
        if (facultyId is null && creatorUserId.HasValue)
        {
            var creator = await Users.GetByIdAsync(creatorUserId.Value);
            facultyId = creator?.FacultyId;
        }

        var studentType = ResolveStudentType(dto.StudentType);
        if (facultyId.HasValue)
        {
            var faculty = await Faculties.GetByIdAsync(facultyId.Value);
            if (faculty is null)
                throw new InvalidOperationException($"Faculty with ID {facultyId.Value} not found.");
        }

        if (dto.SpecializationId.HasValue)
        {
            var spec = await Specializations.GetByIdAsync(dto.SpecializationId.Value);
            if (spec is null)
                throw new SpecializationNotFoundException(dto.SpecializationId.Value);
        }

        var code = dto.StudentCode;
        var email = dto.Email;

        if (string.IsNullOrWhiteSpace(code) && facultyId.HasValue)
            code = await _codeGeneration.GenerateStudentCodeAsync(facultyId.Value, enrollmentDate);

        if (string.IsNullOrWhiteSpace(email))
            email = !string.IsNullOrWhiteSpace(code) ? code + "@intellicampus.online" : dto.Email;

        if (string.IsNullOrWhiteSpace(email))
            throw new InvalidOperationException("Email is required. Provide an email or ensure a faculty is assigned for auto-generation.");

        if (await Users.AnyAsync(u => u.Email == email))
            throw new InvalidOperationException("Email already exists.");

        var student = new Student
        {
            NationalId = dto.NationalId,
            FullName = dto.FullName,
            FullNameAr = dto.FullNameAr,
            PhoneNumber = dto.PhoneNumber,
            Email = email,
            Address = dto.Address,
            Password = _passwordService.HashPassword(password),
            Nationality = dto.Nationality,
            StudentCode = code,
            FacultyId = facultyId,
            StudentType = studentType,
            Level = dto.Level,
            DepartmentId = departmentId,
            BylawId = bylawId,
            EnrollmentDate = enrollmentDate,
            Program = studentType == StudentType.UnderGrad ? dto.Program : StudentProgram.General,
            SpecializationId = dto.SpecializationId
        };

        Students.Add(student);
        await _unitOfWork.SaveChangesAsync();

        var roleName = ResolveStudentRoleName(studentType);
        var role = (await RolesRepo.GetAllAsync()).First(r => r.RoleName == roleName);
        var userRole = new UserRoleJunction
        {
            UserId = student.UserId,
            RoleId = role.RoleId,
            IsActive = true,
            AssignedAt = DateTime.UtcNow
        };
        _unitOfWork.GetRepository<UserRoleJunction, int>().Add(userRole);
        await _unitOfWork.SaveChangesAsync();

        if (student.DepartmentId.HasValue)
        {
            var spec = new StudentSpec(student.UserId);
            var result = await Students.GetByIdAsync(spec);
            return MapToDto(result!);
        }

        return MapToDto(student);
    }

    public async Task<StudentDto> UpdateAsync(int studentId, UpdateStudentDto dto)
    {
        var spec = new StudentSpec(studentId);
        var student = await Students.GetByIdAsync(spec);

        if (student is null)
            throw new StudentNotFoundException(studentId);

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
        if (dto.FacultyId.HasValue) student.FacultyId = dto.FacultyId;
        if (dto.Level.HasValue) student.Level = dto.Level;

        var departmentId = await ResolveDepartmentIdAsync(dto.DepartmentId, dto.DepartmentName);
        if (departmentId.HasValue) student.DepartmentId = departmentId;

        if (dto.BylawId.HasValue) student.BylawId = dto.BylawId;

        if (dto.Program.HasValue && student.StudentType == StudentType.UnderGrad)
            student.Program = dto.Program;
        if (dto.SpecializationId.HasValue) student.SpecializationId = dto.SpecializationId.Value;

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

    public async Task<StudentDto> UpdateLevelAsync(int studentId, int level)
    {
        var student = await Students.GetByIdAsync(studentId);
        if (student is null) throw new StudentNotFoundException(studentId);

        student.Level = level;
        await _unitOfWork.SaveChangesAsync();

        return MapToDto(student);
    }

    public async Task DeleteAsync(int studentId)
    {
        var spec = new StudentSpec(studentId);
        var student = await Students.GetByIdAsync(spec);

        if (student is null)
            throw new StudentNotFoundException(studentId);

        Students.Delete(student);
        await _unitOfWork.SaveChangesAsync();
    }

    private static StudentType ResolveStudentType(string? studentType)
    {
        if (string.IsNullOrWhiteSpace(studentType))
            return StudentType.UnderGrad;

        return studentType.ToLowerInvariant() switch
        {
            "undergrad" or "under_grad" => StudentType.UnderGrad,
            "masters" or "master" => StudentType.Masters,
            "phd" => StudentType.PhD,
            _ => StudentType.UnderGrad
        };
    }

    private static string ResolveStudentRoleName(StudentType studentType)
    {
        return studentType switch
        {
            StudentType.Masters => "Student_Masters",
            StudentType.PhD => "Student_PhD",
            _ => "Student_UnderGrad"
        };
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

        if (matched is null)
            throw new BylawNotFoundException(0);
        return matched.BylawId;
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
            throw int.TryParse(departmentName, out var parsedId) ? new DepartmentNotFoundException(parsedId) : new DepartmentNotFoundException(0);

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
            StudentId = student.UserId,
            UserId = student.UserId,
            NationalId = student.NationalId,
            FullName = student.FullName,
            FullNameAr = student.FullNameAr,
            PhoneNumber = student.PhoneNumber,
            Email = student.Email,
            Address = student.Address,
            Nationality = student.Nationality,
            StudentCode = student.StudentCode,
            FacultyId = student.FacultyId,
            FacultyName = student.Faculty?.FacultyName,
            Level = student.Level,
            DepartmentId = student.DepartmentId,
            DepartmentName = student.Department?.DepartmentName,
            BylawId = student.BylawId,
            BylawName = student.Bylaw?.Name,
            BylawVersion = student.Bylaw?.Version,
            EnrollmentDate = student.EnrollmentDate?.ToString("dd MM yyyy"),
            Gpa = student.Gpa,
            Program = student.Program,
            SpecializationId = student.SpecializationId,
            SpecializationName = student.Specialization?.Name,
            StudentType = student.StudentType,
            Roles = student.UserRoles.Where(ur => ur.IsActive).Select(ur => ur.Role.RoleName).ToList()
        };
    }
}
