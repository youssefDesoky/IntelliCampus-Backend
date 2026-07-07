using System.Globalization;
using IntelliCampus.shared.Pagination;
using IntelliCampus.Shared.Dtos.Note;
using IntelliCampus.Shared.Dtos.Student;
using IntelliCampus.Shared.Params;
using IntelliCampus.Service_Abstraction;
using IntelliCampus.Domain.Entities;
using IntelliCampus.Domain.Entities.Enums;
using IntelliCampus.Domain.Interfaces;
using IntelliCampus.Service.Specifications;
using IntelliCampus.Service.Exceptions;
using IntelliCampus.Service.Resolvers;

namespace IntelliCampus.Service;

public class StudentService : IStudentService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordService _passwordService;
    private readonly ICodeGenerationService _codeGeneration;
    private readonly UrlResolver _urlResolver;
    private readonly IBylawService _bylawService;
    private readonly IGradeService _gradeService;
    private readonly ICurrentAdminContext _adminContext;

    public StudentService(IUnitOfWork unitOfWork, IPasswordService passwordService, ICodeGenerationService codeGeneration, UrlResolver urlResolver, IBylawService bylawService, IGradeService gradeService, ICurrentAdminContext adminContext)
    {
        _unitOfWork = unitOfWork;
        _passwordService = passwordService;
        _codeGeneration = codeGeneration;
        _urlResolver = urlResolver;
        _bylawService = bylawService;
        _gradeService = gradeService;
        _adminContext = adminContext;
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

    public async Task<StudentDto> GetByIdAsync(int studentId)
    {
        var spec = new StudentSpec(new CourseQueryParams { StudentId = studentId, IncludeCourses = true });
        var student = await Students.GetByIdAsync(spec);

        if (student is null)
            throw new StudentNotFoundException(studentId);

        await _adminContext.EnsureCanAccessFacultyAsync(student.User.FacultyId);
        if (_adminContext.AdminStudentType.HasValue && student.StudentType != _adminContext.AdminStudentType.Value)
            throw new ForbiddenException("You can only manage students of your assigned type.");

        var effectiveCredits = student.BylawId is not null
            ? await _bylawService.GetEffectiveCreditHoursAsync(student.BylawId.Value, student.DepartmentId)
            : new Dictionary<int, int>();

        var gpa = await _gradeService.GetCumulativeGpaAsync(studentId);
        return MapToDto(student, effectiveCredits, gpa);
    }

    public async Task<PaginatedResult<StudentDto>> GetAllAsync(StudentQueryParams queryParams)
    {
        if (_adminContext.IsAdmin)
        {
            queryParams.FacultyId = await _adminContext.GetFacultyIdAsync();
            if (_adminContext.AdminStudentType.HasValue)
                queryParams.Status = _adminContext.AdminStudentType.Value.ToString();
        }

        var spec = new StudentSpec(queryParams);
        var students = await Students.GetAllAsync(spec, asNoTracking: true);
        var dataToReturn = students.Select(s => MapToDto(s)).ToList();

        var countSpec = new StudentCountSpec(queryParams);
        var totalCount = await Students.CountAsync(countSpec);

        return new PaginatedResult<StudentDto>(queryParams.PageIndex, dataToReturn.Count, totalCount, dataToReturn);
    }

    public async Task<StudentDto> CreateAsync(CreateStudentDto dto, int? creatorUserId = null)
    {
        await _adminContext.EnsureAdminHasFacultyAsync();

        if (await Users.AnyAsync(u => u.NationalId == dto.NationalId))
            throw new InvalidOperationException("National ID already exists.");

        var departmentId = await ResolveDepartmentIdAsync(dto.DepartmentId, dto.DepartmentName);
        var bylawId = await ResolveBylawIdAsync(dto.BylawId, dto.BylawName);
        var enrollmentDate = ParseEnrollmentDate(dto.EnrollmentDate) ?? EgyptTime.Now;
        var facultyId = dto.FacultyId;
        if (facultyId is null && creatorUserId.HasValue)
        {
            var creator = await Users.GetByIdAsync(creatorUserId.Value);
            facultyId = creator?.FacultyId;
        }

        var studentType = ResolveStudentType(dto.StudentType);
        await ValidateBylawTypeMatch(bylawId, studentType);

        if (facultyId.HasValue)
        {
            var faculty = await Faculties.GetByIdAsync(facultyId.Value);
            if (faculty is null)
                throw new InvalidOperationException($"Faculty with ID {facultyId.Value} not found.");
        }

        var (code, email, password) = await ResolveStudentCodeEmailPasswordAsync(dto, facultyId, enrollmentDate);

        var student = BuildStudentEntity(dto, email, password, departmentId, facultyId, enrollmentDate, code, bylawId, studentType);

        Students.Add(student);
        await _unitOfWork.SaveChangesAsync();

        var roleName = ResolveStudentRoleName(studentType);
        await AssignStudentRoleAsync(student.UserId, roleName);

        if (student.DepartmentId.HasValue)
        {
            var spec = new StudentSpec(new CourseQueryParams { StudentId = student.UserId });
            var result = await Students.GetByIdAsync(spec);
            return MapToDto(result!);
        }

        return MapToDto(student);
    }

    public async Task<StudentDto> UpdateAsync(int studentId, UpdateStudentDto dto)
    {
        var spec = new StudentSpec(new CourseQueryParams { StudentId = studentId });
        var student = await Students.GetByIdAsync(spec);

        if (student is null)
            throw new StudentNotFoundException(studentId);

        await _adminContext.EnsureCanAccessFacultyAsync(student.User.FacultyId);
        if (_adminContext.AdminStudentType.HasValue && student.StudentType != _adminContext.AdminStudentType.Value)
            throw new ForbiddenException("You can only manage students of your assigned type.");

        if (dto.Email is not null && dto.Email != student.User.Email)
        {
            if (await Users.AnyAsync(u => u.Email == dto.Email && u.UserId != studentId))
                throw new InvalidOperationException("Email already exists.");
            student.User.Email = dto.Email;
        }

        if (dto.FullName is not null) student.User.FullName = dto.FullName;
        if (dto.FullNameAr is not null) student.User.FullNameAr = dto.FullNameAr;
        if (dto.PhoneNumber is not null) student.User.PhoneNumber = dto.PhoneNumber;
        if (dto.Address is not null) student.User.Address = dto.Address;
        if (dto.Nationality is not null) student.User.Nationality = dto.Nationality;
        if (dto.StudentCode is not null) student.StudentCode = dto.StudentCode;
        if (dto.FacultyId.HasValue) student.User.FacultyId = dto.FacultyId;
        if (dto.Level.HasValue) student.Level = dto.Level;

        var departmentId = await ResolveDepartmentIdAsync(dto.DepartmentId, dto.DepartmentName);
        if (departmentId.HasValue) student.DepartmentId = departmentId;

        if (dto.BylawId.HasValue)
        {
            await ValidateBylawTypeMatch(dto.BylawId, student.StudentType);
            student.BylawId = dto.BylawId;
        }

        if (dto.Program.HasValue)
            student.Program = dto.Program;
        if (dto.ProfileImage is not null) student.User.ProfileImage = dto.ProfileImage == "" ? null : dto.ProfileImage;

        var enrollmentDate = ParseEnrollmentDate(dto.EnrollmentDate);
        if (enrollmentDate.HasValue) student.EnrollmentDate = enrollmentDate.Value;

        Students.Update(student);
        await _unitOfWork.SaveChangesAsync();

        var gpa = await _gradeService.GetCumulativeGpaAsync(student.UserId);

        if (student.DepartmentId.HasValue)
        {
            var updatedSpec = new StudentSpec(new CourseQueryParams { StudentId = student.UserId });
            var result = await Students.GetByIdAsync(updatedSpec);
            return MapToDto(result!, gpa: gpa);
        }

        return MapToDto(student, gpa: gpa);
    }

    public async Task<StudentDto> UpdateLevelAsync(int studentId, int level)
    {
        var student = await Students.GetByIdAsync(studentId);
        if (student is null) throw new StudentNotFoundException(studentId);

        await _adminContext.EnsureCanAccessFacultyAsync(student.User.FacultyId);
        if (_adminContext.AdminStudentType.HasValue && student.StudentType != _adminContext.AdminStudentType.Value)
            throw new ForbiddenException("You can only manage students of your assigned type.");

        student.Level = level;
        await _unitOfWork.SaveChangesAsync();

        return MapToDto(student);
    }

    public async Task DeleteAsync(int studentId)
    {
        var spec = new StudentSpec(new CourseQueryParams { StudentId = studentId });
        var student = await Students.GetByIdAsync(spec);

        if (student is null)
            throw new StudentNotFoundException(studentId);

        await _adminContext.EnsureCanAccessFacultyAsync(student.User.FacultyId);
        if (_adminContext.AdminStudentType.HasValue && student.StudentType != _adminContext.AdminStudentType.Value)
            throw new ForbiddenException("You can only manage students of your assigned type.");

        var user = student.User;
        Students.Delete(student);

        if (user is not null)
            Users.Delete(user);

        await _unitOfWork.SaveChangesAsync();
    }

    private static BylawType ToBylawType(StudentType studentType) => studentType switch
    {
        StudentType.Bachelor => BylawType.Bachelor,
        StudentType.Masters => BylawType.Master,
        StudentType.PhD => BylawType.PhD,
        StudentType.Diploma => BylawType.Diploma,
        _ => BylawType.Bachelor
    };

    private async Task ValidateBylawTypeMatch(int? bylawId, StudentType studentType)
    {
        if (!bylawId.HasValue)
            return;

        var bylaw = await Bylaws.GetByIdAsync(bylawId.Value);
        if (bylaw is null)
            throw new BylawNotFoundException(bylawId.Value);

        var expectedType = ToBylawType(studentType);
        if (bylaw.Type != expectedType)
            throw new InvalidOperationException(
                $"Bylaw type '{bylaw.Type}' does not match student type '{studentType}'. " +
                $"Expected a '{expectedType}' bylaw.");
    }

    private static StudentType ResolveStudentType(string? studentType)
    {
        if (string.IsNullOrWhiteSpace(studentType))
            return StudentType.Bachelor;

        return studentType.ToLowerInvariant() switch
        {
            "Bachelor" or "bachelor" => StudentType.Bachelor,
            "masters" or "master" => StudentType.Masters,
            "phd" => StudentType.PhD,
            "diploma" => StudentType.Diploma,
            _ => StudentType.Bachelor
        };
    }

    private static string ResolveStudentRoleName(StudentType studentType)
    {
        return studentType switch
        {
            StudentType.Masters => "Student_Masters",
            StudentType.PhD => "Student_PhD",
            StudentType.Diploma => "Student_Diploma",
            _ => "Student_Bachelor"
        };
    }

    private async Task<int?> ResolveBylawIdAsync(int? bylawId, string? bylawName)
    {
        if (bylawId.HasValue)
            return bylawId;

        if (string.IsNullOrWhiteSpace(bylawName))
            return null;

        var bylaws = await Bylaws.GetAllAsync(new BylawSpec(), asNoTracking: true);
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
        var department = (await Departments.GetAllAsync(paramSpec, asNoTracking: true)).FirstOrDefault();

        if (department is not null)
            return department.DepartmentId;

        var normalized = departmentName.Trim();
        var departments = await Departments.GetAllAsync(new DepartmentSpec(), asNoTracking: true);
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

    private StudentDto MapToDto(Student student, Dictionary<int, int>? effectiveCredits = null, double? gpa = null)
    {
        gpa ??= student.Gpa;
        return new StudentDto
        {
            StudentId = student.UserId,
            UserId = student.UserId,
            NationalId = student.User.NationalId,
            FullName = student.User.FullName,
            FullNameAr = student.User.FullNameAr,
            PhoneNumber = student.User.PhoneNumber,
            Email = student.User.Email,
            Address = student.User.Address,
            Nationality = student.User.Nationality,
            StudentCode = student.StudentCode,
            FacultyId = student.User.FacultyId,
            FacultyName = student.User.Faculty?.FacultyName,
            FacultyNameAr = student.User.Faculty?.FacultyNameAr,
            Level = student.Level,
            DepartmentId = student.DepartmentId,
            DepartmentName = student.Department?.DepartmentName,
            DepartmentNameAr = student.Department?.DepartmentNameAr,
            BylawId = student.BylawId,
            BylawName = student.Bylaw?.Name,
            BylawNameAr = student.Bylaw?.NameAr,
            EnrollmentDate = student.EnrollmentDate?.ToString("dd MM yyyy"),
            Gpa = gpa.Value,
            ProbationThreshold = student.Bylaw?.Settings.ProbationThreshold,
            IsOnProbation = gpa > 0
                && student.Bylaw?.Settings.ProbationThreshold is not null
                && (decimal)gpa < student.Bylaw.Settings.ProbationThreshold.Value,
            Program = student.Program,
            StudentType = student.StudentType,
            ProfileImage = _urlResolver.ResolveProfile(student.User.ProfileImage),
            Roles = student.User!.UserRoles.Where(ur => ur.IsActive).Select(ur => ur.Role.RoleName).ToList(),
            Courses = student.StudentCourses?.Select(sc => new StudentCourseDto
            {
                Id = sc.CourseId,
                Title = sc.Course.CourseName,
                CourseName = sc.Course.CourseName,
                CreditHours = effectiveCredits?.GetValueOrDefault(sc.CourseId, sc.Course.CreditHours) ?? sc.Course.CreditHours,
                Status = sc.Status.ToString(),
                Notes = sc.Course.Notes
                    .Where(n => n.StudentId == student.UserId)
                    .Select(n => new StudentCourseNoteDto
                    {
                        Id = n.NoteId,
                        Title = n.Title,
                        Content = n.Content,
                        CreationDate = n.CreatedAt.ToString("MMM dd, yyyy"),
                        Modified = n.ModifiedAt.HasValue
                            ? n.ModifiedAt.Value.ToString("MMM dd, yyyy, h:mm tt")
                            : n.CreatedAt.ToString("MMM dd, yyyy, h:mm tt"),
                        LinkedLecture = n.MaterialFolder is not null
                            ? MapLinkedLecture(n.MaterialFolder)
                            : null,
                        AiSummary = n.NoteSummary?.GeneratedText
                    }).ToList()
            }).ToList()
        };
    }

    private static LinkedLectureDto MapLinkedLecture(MaterialFolder folder)
    {
        return new LinkedLectureDto
        {
            Id = folder.MaterialFolderId,
            Title = folder.Name,
            ShortTitle = folder.Name,
            WeekLabel = folder.Name + " Lecture",
            Description = folder.Description,
            CourseId = folder.CourseId,
            MaterialFolderName = folder.Name
        };
    }

    private async Task<(string Code, string Email, string Password)> ResolveStudentCodeEmailPasswordAsync(
        CreateStudentDto dto, int? facultyId, DateTime enrollmentDate)
    {
        var password = string.IsNullOrWhiteSpace(dto.Password) ? dto.NationalId : dto.Password;
        var code = dto.StudentCode;
        var email = dto.Email;

        if (string.IsNullOrWhiteSpace(code) && facultyId.HasValue)
            code = await _codeGeneration.GenerateStudentCodeAsync(facultyId.Value, enrollmentDate, ResolveStudentType(dto.StudentType));

        if (string.IsNullOrWhiteSpace(email))
            email = !string.IsNullOrWhiteSpace(code) ? code + "@intellicampus.online" : dto.Email;

        if (string.IsNullOrWhiteSpace(email))
            throw new InvalidOperationException("Email is required. Provide an email or ensure a faculty is assigned for auto-generation.");

        if (await Users.AnyAsync(u => u.Email == email))
            throw new InvalidOperationException("Email already exists.");

        return (code!, email!, password);
    }

    private Student BuildStudentEntity(CreateStudentDto dto, string email, string password, int? departmentId, int? facultyId, DateTime enrollmentDate, string code, int? bylawId, StudentType studentType)
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
            MustChangePassword = true,
            FacultyId = facultyId,
            ProfileImage = dto.ProfileImage
        };

        return new Student
        {
            User = user,
            StudentCode = code,
            StudentType = studentType,
            Level = dto.Level,
            DepartmentId = departmentId,
            BylawId = bylawId,
            EnrollmentDate = enrollmentDate,
            Program = dto.Program
        };
    }

    private async Task AssignStudentRoleAsync(int userId, string roleName)
    {
        var role = await RolesRepo.GetByIdAsync(new RoleByNameSpec(roleName))
            ?? throw new InvalidOperationException($"Role '{roleName}' not found.");
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