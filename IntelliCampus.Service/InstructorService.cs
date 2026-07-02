using IntelliCampus.Domain.Entities;
using IntelliCampus.Domain.Entities.Enums;
using IntelliCampus.Domain.Interfaces;
using IntelliCampus.Service_Abstraction;
using IntelliCampus.shared.Pagination;
using IntelliCampus.Shared.Dtos.Instructor;
using IntelliCampus.Shared.Params;
using System.Globalization;
using IntelliCampus.Service.Specifications;
using IntelliCampus.Service.Exceptions;
using IntelliCampus.Service.Resolvers;

namespace IntelliCampus.Service;

public class InstructorService(IUnitOfWork unitOfWork, IPasswordService passwordService, ICodeGenerationService codeGeneration, UrlResolver urlResolver) : IInstructorService
{
    private readonly IPasswordService _passwordService = passwordService;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly ICodeGenerationService _codeGeneration = codeGeneration;
    private readonly UrlResolver _urlResolver = urlResolver;

    private IGenericRepository<Instructor, int> Instructors
        => _unitOfWork.GetRepository<Instructor, int>();

    private IGenericRepository<User, int> Users
        => _unitOfWork.GetRepository<User, int>();

    private IGenericRepository<Department, int> Departments
        => _unitOfWork.GetRepository<Department, int>();

    private IGenericRepository<Role, int> RolesRepo
        => _unitOfWork.GetRepository<Role, int>();

    public async Task<InstructorDto> GetByIdAsync(int instructorId)
    {
        var spec = new InstructorSpec(instructorId);
        var instructor = await Instructors.GetByIdAsync(spec);

        if (instructor is null)
            throw new InstructorNotFoundException(instructorId);

        return MapToDto(instructor);
    }

    public async Task<PaginatedResult<InstructorDto>> GetAllAsync(InstructorQueryParams queryParams)
    {
        var spec = new InstructorSpec(queryParams);
        var instructors = await Instructors.GetAllAsync(spec, asNoTracking: true);
        var dataToReturn = instructors.Select(MapToDto).ToList();

        var countSpec = new InstructorCountSpec(queryParams);
        var totalCount = await Instructors.CountAsync(countSpec);

        return new PaginatedResult<InstructorDto>(queryParams.PageIndex, dataToReturn.Count, totalCount, dataToReturn);
    }

    public async Task<IEnumerable<InstructorDto>> GetProfessorsAsync(InstructorQueryParams queryParams)
    {
        var spec = new ProfessorsSpec(queryParams);
        var professors = await Instructors.GetAllAsync(spec, asNoTracking: true);
        return professors.Select(MapToDto);
    }

    public async Task<InstructorDto> CreateAsync(CreateInstructorDto dto, int? creatorUserId = null)
    {
        if (await Users.AnyAsync(u => u.NationalId == dto.NationalId))
            throw new InvalidOperationException("National ID already exists.");

        var departmentId = await ResolveDepartmentIdAsync(dto.DepartmentName);
        var hireDate = ParseDate(dto.HireDate) ?? EgyptTime.Now;
        var password = string.IsNullOrWhiteSpace(dto.Password) ? dto.NationalId : dto.Password;

        var facultyId = dto.FacultyId;
        if (facultyId is null && creatorUserId.HasValue)
        {
            var creator = await Users.GetByIdAsync(creatorUserId.Value);
            facultyId = creator?.FacultyId;
        }

        var (code, email) = await ResolveInstructorCodeAndEmailAsync(dto, facultyId, hireDate);

        var instructor = BuildInstructorEntity(dto, email, password, departmentId, facultyId, hireDate, code);

        Instructors.Add(instructor);
        await _unitOfWork.SaveChangesAsync();

        if (dto.LoanFromDepartmentId.HasValue || dto.LoanFromFacultyId.HasValue || dto.LoanProfessorId is not null)
        {
            var loanInstructor = new LoanInstructor
            {
                UserId = instructor.UserId,
                LoanFromDepartmentId = dto.LoanFromDepartmentId,
                LoanFromFacultyId = dto.LoanFromFacultyId,
                LoanProfessorId = dto.LoanProfessorId
            };
            _unitOfWork.GetRepository<LoanInstructor, int>().Add(loanInstructor);
        }

        await AssignInstructorRoleAsync(instructor.UserId);

        if (instructor.DepartmentId.HasValue)
        {
            var spec = new InstructorSpec(instructor.UserId);
            var result = await Instructors.GetByIdAsync(spec);
            return MapToDto(result!);
        }

        return MapToDto(instructor);
    }

    public async Task<InstructorDto> UpdateAsync(int instructorId, UpdateInstructorDto dto)
    {
        var spec = new InstructorSpec(instructorId);
        var instructor = await Instructors.GetByIdAsync(spec);

        if (instructor is null)
            throw new InstructorNotFoundException(instructorId);

        if (dto.Email is not null && dto.Email != instructor.User.Email)
        {
            if (await Users.AnyAsync(u => u.Email == dto.Email && u.UserId != instructorId))
                throw new InvalidOperationException("Email already exists.");
            instructor.User.Email = dto.Email;
        }

        if (dto.FullName is not null) instructor.User.FullName = dto.FullName;
        if (dto.FullNameAr is not null) instructor.User.FullNameAr = dto.FullNameAr;
        if (dto.PhoneNumber is not null) instructor.User.PhoneNumber = dto.PhoneNumber;
        if (dto.Address is not null) instructor.User.Address = dto.Address;
        if (dto.Nationality is not null) instructor.User.Nationality = dto.Nationality;
        if (dto.InstructorCode is not null) instructor.InstructorCode = dto.InstructorCode;
        if (dto.InstructorRole is not null) instructor.InstructorRole = ParseInstructorRole(dto.InstructorRole);
        if (dto.Specialization is not null) instructor.Specialization = dto.Specialization;
        if (dto.FacultyId.HasValue) instructor.User.FacultyId = dto.FacultyId;
        if (dto.Status is not null) instructor.Status = ParseStatus(dto.Status);
        if (dto.OfficeHoursRoomId.HasValue) instructor.OfficeHoursRoomId = dto.OfficeHoursRoomId;
        if (dto.ProfileImage is not null) instructor.User.ProfileImage = dto.ProfileImage;
        if (dto.Secondment is not null) instructor.Secondment = dto.Secondment;

        var loanInstructor = await _unitOfWork.GetRepository<LoanInstructor, int>().GetByIdAsync(instructor.UserId);
        if (loanInstructor is not null)
        {
            if (dto.LoanFromDepartmentId.HasValue) loanInstructor.LoanFromDepartmentId = dto.LoanFromDepartmentId;
            if (dto.LoanFromFacultyId.HasValue) loanInstructor.LoanFromFacultyId = dto.LoanFromFacultyId;
            if (dto.LoanProfessorId is not null) loanInstructor.LoanProfessorId = dto.LoanProfessorId;
        }

        var contractStartDate = ParseDate(dto.ContractStartDate);
        if (contractStartDate.HasValue) instructor.ContractStartDate = contractStartDate.Value;

        var contractEndDate = ParseDate(dto.ContractEndDate);
        if (contractEndDate.HasValue) instructor.ContractEndDate = contractEndDate.Value;

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

    public async Task DeleteAsync(int instructorId)
    {
        var spec = new InstructorSpec(instructorId);
        var instructor = await Instructors.GetByIdAsync(spec);

        if (instructor is null)
            throw new InstructorNotFoundException(instructorId);

        Instructors.Delete(instructor);
        await _unitOfWork.SaveChangesAsync();
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
        var department = (await Departments.GetAllAsync(paramSpec, asNoTracking: true)).FirstOrDefault();

        if (department is not null)
            return department.DepartmentId;

        var normalized = departmentName.Trim();
        var departments = await Departments.GetAllAsync(new DepartmentSpec(), asNoTracking: true);
        var matched = departments.FirstOrDefault(d =>
            string.Equals(GetDepartmentCode(d.DepartmentName), normalized, StringComparison.OrdinalIgnoreCase));

        if (matched is null)
            throw int.TryParse(departmentName, out var parsedId) ? new DepartmentNotFoundException(parsedId) : new DepartmentNotFoundException(0);

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

    private InstructorDto MapToDto(Instructor instructor)
    {
        return new InstructorDto
        {
            InstructorId = instructor.UserId,
            UserId = instructor.UserId,
            NationalId = instructor.User.NationalId,
            FullName = instructor.User.FullName,
            FullNameAr = instructor.User.FullNameAr,
            PhoneNumber = instructor.User.PhoneNumber,
            Email = instructor.User.Email,
            Address = instructor.User.Address,
            Nationality = instructor.User.Nationality,
            InstructorCode = instructor.InstructorCode,
            InstructorRole = instructor.InstructorRole?.ToString(),
            Specialization = instructor.Specialization,
            DepartmentId = instructor.DepartmentId,
            DepartmentName = instructor.Department?.DepartmentName,
            HireDate = instructor.HireDate?.ToString("dd MM yyyy"),
            FacultyId = instructor.User.FacultyId,
            FacultyName = instructor.User.Faculty?.FacultyName,
            Status = instructor.Status?.ToString(),
            OfficeHoursRoomId = instructor.OfficeHoursRoomId,
            OfficeHoursRoomName = instructor.OfficeHoursRoom?.RoomName,
            ProfileImage = _urlResolver.ResolveProfile(instructor.User.ProfileImage),
            ContractStartDate = instructor.ContractStartDate?.ToString("dd MM yyyy"),
            ContractEndDate = instructor.ContractEndDate?.ToString("dd MM yyyy"),
            Secondment = instructor.Secondment,
            Roles = instructor.User!.UserRoles.Where(ur => ur.IsActive).Select(ur => ur.Role.RoleName).ToList()
        };
    }

    private async Task<(string Code, string Email)> ResolveInstructorCodeAndEmailAsync(
        CreateInstructorDto dto, int? facultyId, DateTime hireDate)
    {
        var code = dto.InstructorCode;
        var email = dto.Email;

        if (string.IsNullOrWhiteSpace(code) && facultyId.HasValue)
            code = await _codeGeneration.GenerateInstructorCodeAsync(facultyId.Value, hireDate);

        if (string.IsNullOrWhiteSpace(email))
            email = !string.IsNullOrWhiteSpace(code) ? code + "@intellicampus.online" : dto.Email;

        if (string.IsNullOrWhiteSpace(email))
            throw new InvalidOperationException("Email is required. Provide an email or ensure a faculty is assigned for auto-generation.");

        if (await Users.AnyAsync(u => u.Email == email))
            throw new InvalidOperationException("Email already exists.");

        return (code!, email!);
    }

    private Instructor BuildInstructorEntity(CreateInstructorDto dto, string email, string password, int? departmentId, int? facultyId, DateTime hireDate, string code)
    {
        var user = new User
        {
            NationalId = dto.NationalId,
            FullName = dto.FullName,
            FullNameAr = dto.FullNameAr,
            PhoneNumber = dto.PhoneNumber,
            Email = email,
            Address = dto.Address,
            Password = _passwordService.HashPassword(password),
            Nationality = dto.Nationality,
            FacultyId = facultyId,
            ProfileImage = dto.ProfileImage
        };

        return new Instructor
        {
            User = user,
            InstructorCode = code,
            InstructorRole = ParseInstructorRole(dto.InstructorRole),
            Specialization = dto.Specialization,
            DepartmentId = departmentId,
            HireDate = hireDate,
            Status = ParseStatus(dto.Status),
            OfficeHoursRoomId = dto.OfficeHoursRoomId,
            ContractStartDate = ParseDate(dto.ContractStartDate),
            ContractEndDate = ParseDate(dto.ContractEndDate),
            Secondment = dto.Secondment
        };
    }

    private async Task AssignInstructorRoleAsync(int userId)
    {
        var role = await RolesRepo.GetByIdAsync(new RoleByNameSpec("Instructor"))
            ?? throw new InvalidOperationException("Role 'Instructor' not found.");
        var userRole = new UserRoleJunction
        {
            UserId = userId,
            RoleId = role.RoleId,
            IsActive = true,
            AssignedAt = EgyptTime.Now
        };
        _unitOfWork.GetRepository<UserRoleJunction, int>().Add(userRole);
        await _unitOfWork.SaveChangesAsync();
    }

}
