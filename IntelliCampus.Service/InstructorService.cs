using IntelliCampus.Domain.Entities;
using IntelliCampus.Domain.Entities.Enums;
using IntelliCampus.Domain.Interfaces;
using IntelliCampus.Service_Abstraction;
using IntelliCampus.Shared.Dtos.Instructor;
using System.Globalization;
using IntelliCampus.Service.Specifications;

namespace IntelliCampus.Service;

public class InstructorService(IUnitOfWork unitOfWork, IPasswordService passwordService) : IInstructorService
{
    private readonly IPasswordService _passwordService = passwordService;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;

    private IGenericRepository<Instructor, int> Instructors
        => _unitOfWork.GetRepository<Instructor, int>();

    private IGenericRepository<User, int> Users
        => _unitOfWork.GetRepository<User, int>();

    private IGenericRepository<Department, int> Departments
        => _unitOfWork.GetRepository<Department, int>();

    private IGenericRepository<Role, int> RolesRepo
        => _unitOfWork.GetRepository<Role, int>();

    public async Task<InstructorDto?> GetByIdAsync(int instructorId)
    {
        var spec = new InstructorSpec(instructorId);
        var instructor = await Instructors.GetByIdAsync(spec);

        if (instructor is null)
            return null;

        return MapToDto(instructor);
    }

    public async Task<IEnumerable<InstructorDto>> GetAllAsync()
    {
        var spec = new InstructorSpec();
        var instructors = await Instructors.GetAllAsync(spec);

        return instructors.Select(MapToDto);
    }

    public async Task<InstructorDto> CreateAsync(CreateInstructorDto dto, int? creatorUserId = null)
    {
        if (await Users.AnyAsync(u => u.Email == dto.Email))
            throw new InvalidOperationException("Email already exists.");

        if (await Users.AnyAsync(u => u.NationalId == dto.NationalId))
            throw new InvalidOperationException("National ID already exists.");

        var departmentId = await ResolveDepartmentIdAsync(dto.DepartmentName);
        var hireDate = ParseDate(dto.HireDate) ?? DateTime.UtcNow;
        var password = string.IsNullOrWhiteSpace(dto.Password) ? "Instructor@123" : dto.Password;

        var facultyId = dto.FacultyId;
        if (facultyId is null && creatorUserId.HasValue)
        {
            var creator = await Users.GetByIdAsync(creatorUserId.Value);
            facultyId = creator?.FacultyId;
        }

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
            InstructorCode = dto.InstructorCode,
            InstructorRole = ParseInstructorRole(dto.InstructorRole),
            Specialization = dto.Specialization,
            DepartmentId = departmentId,
            HireDate = hireDate,
            FacultyId = facultyId,
            Status = ParseStatus(dto.Status),
            OfficeHoursRoomId = dto.OfficeHoursRoomId
        };

        Instructors.Add(instructor);
        await _unitOfWork.SaveChangesAsync();

        var role = (await RolesRepo.GetAllAsync()).First(r => r.RoleName == "Instructor");
        var userRole = new UserRoleJunction
        {
            UserId = instructor.UserId,
            RoleId = role.RoleId,
            IsActive = true,
            AssignedAt = DateTime.UtcNow
        };
        _unitOfWork.GetRepository<UserRoleJunction, int>().Add(userRole);
        await _unitOfWork.SaveChangesAsync();

        if (instructor.DepartmentId.HasValue)
        {
            var spec = new InstructorSpec(instructor.UserId);
            var result = await Instructors.GetByIdAsync(spec);
            return MapToDto(result!);
        }

        return MapToDto(instructor);
    }

    public async Task<InstructorDto?> UpdateAsync(int instructorId, UpdateInstructorDto dto)
    {
        var spec = new InstructorSpec(instructorId);
        var instructor = await Instructors.GetByIdAsync(spec);

        if (instructor is null)
            return null;

        if (dto.Email is not null && dto.Email != instructor.Email)
        {
            if (await Users.AnyAsync(u => u.Email == dto.Email && u.UserId != instructorId))
                throw new InvalidOperationException("Email already exists.");
            instructor.Email = dto.Email;
        }

        if (dto.FullName is not null) instructor.FullName = dto.FullName;
        if (dto.FullNameAr is not null) instructor.FullNameAr = dto.FullNameAr;
        if (dto.PhoneNumber is not null) instructor.PhoneNumber = dto.PhoneNumber;
        if (dto.Address is not null) instructor.Address = dto.Address;
        if (dto.Nationality is not null) instructor.Nationality = dto.Nationality;
        if (dto.InstructorCode is not null) instructor.InstructorCode = dto.InstructorCode;
        if (dto.InstructorRole is not null) instructor.InstructorRole = ParseInstructorRole(dto.InstructorRole);
        if (dto.Specialization is not null) instructor.Specialization = dto.Specialization;
        if (dto.FacultyId.HasValue) instructor.FacultyId = dto.FacultyId;
        if (dto.Status is not null) instructor.Status = ParseStatus(dto.Status);
        if (dto.OfficeHoursRoomId.HasValue) instructor.OfficeHoursRoomId = dto.OfficeHoursRoomId;

        var departmentId = await ResolveDepartmentIdAsync(dto.DepartmentName);
        if (departmentId.HasValue) instructor.DepartmentId = departmentId;

        var hireDate = ParseDate(dto.HireDate);
        if (hireDate.HasValue) instructor.HireDate = hireDate.Value;

        await _unitOfWork.SaveChangesAsync();

        if (instructor.DepartmentId.HasValue)
        {
            var updatedSpec = new InstructorSpec(instructor.UserId);
            var result = await Instructors.GetByIdAsync(updatedSpec);
            return MapToDto(result!);
        }

        return MapToDto(instructor);
    }

    private static InstructorRole? ParseInstructorRole(string? role)
    {
        if (string.IsNullOrWhiteSpace(role))
            return null;

        return role.ToLowerInvariant() switch
        {
            "ta" or "teachingassistant" or "teaching_assistant" or "معيد" => InstructorRole.TeachingAssistant,
            "lecturer" or "مدرس" => InstructorRole.Lecturer,
            "assistantlecturer" or "assistant_lecturer" or "مدرس مساعد" => InstructorRole.AssistantLecturer,
            "associateprofessor" or "associate_professor" or "أستاذ مساعد" => InstructorRole.AssociateProfessor,
            "professor" or "أستاذ" => InstructorRole.Professor,
            _ => null
        };
    }

    private static InstructorStatus? ParseStatus(string? status)
    {
        if (string.IsNullOrWhiteSpace(status))
            return null;

        return status.ToLowerInvariant() switch
        {
            "employed" or "متعين" => InstructorStatus.Employed,
            "loan" or "اعارة" or "إعارة" => InstructorStatus.Loan,
            _ => null
        };
    }

    public async Task<bool> DeleteAsync(int instructorId)
    {
        var spec = new InstructorSpec(instructorId);
        var instructor = await Instructors.GetByIdAsync(spec);

        if (instructor is null)
            return false;

        Instructors.Delete(instructor);
        await _unitOfWork.SaveChangesAsync();

        return true;
    }

    private async Task<int?> ResolveDepartmentIdAsync(string? departmentName)
    {
        if (string.IsNullOrWhiteSpace(departmentName))
            return null;

        if (int.TryParse(departmentName, out var id))
        {
            if (await Departments.AnyAsync(d => d.DepartmentId == id))
                return id;
        }

        var paramSpec = new DepartmentByNameSpec(departmentName);
        var department = (await Departments.GetAllAsync(paramSpec)).FirstOrDefault();

        if (department is not null)
            return department.DepartmentId;

        var normalized = departmentName.Trim();
        var departments = await Departments.GetAllAsync();
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
            InstructorRole = instructor.InstructorRole?.ToString(),
            Specialization = instructor.Specialization,
            DepartmentId = instructor.DepartmentId,
            DepartmentName = instructor.Department?.DepartmentName,
            HireDate = instructor.HireDate,
            FacultyId = instructor.FacultyId,
            FacultyName = instructor.Faculty?.FacultyName,
            Status = instructor.Status?.ToString(),
            OfficeHoursRoomId = instructor.OfficeHoursRoomId,
            OfficeHoursRoomName = instructor.OfficeHoursRoom?.RoomName,
            Roles = instructor.UserRoles.Where(ur => ur.IsActive).Select(ur => ur.Role.RoleName).ToList()
        };
    }
}
