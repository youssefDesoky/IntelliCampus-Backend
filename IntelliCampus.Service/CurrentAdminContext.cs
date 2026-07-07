using System.Security.Claims;
using IntelliCampus.Domain.Entities;
using IntelliCampus.Domain.Entities.Enums;
using IntelliCampus.Domain.Interfaces;
using IntelliCampus.Service.Exceptions;
using IntelliCampus.Service_Abstraction;
using Microsoft.AspNetCore.Http;

namespace IntelliCampus.Service;

public class CurrentAdminContext : ICurrentAdminContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IUnitOfWork _unitOfWork;
    private int? _facultyId;
    private bool _facultyLoaded;

    public CurrentAdminContext(IHttpContextAccessor httpContextAccessor, IUnitOfWork unitOfWork)
    {
        _httpContextAccessor = httpContextAccessor;
        _unitOfWork = unitOfWork;
    }

    public int? UserId
    {
        get
        {
            var claim = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier);
            return claim is not null && int.TryParse(claim.Value, out var id) ? id : null;
        }
    }

    public IEnumerable<string> Roles
    {
        get
        {
            var user = _httpContextAccessor.HttpContext?.User;
            return user is null
                ? []
                : user.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();
        }
    }

    public bool IsSuperAdmin => Roles.Contains(nameof(UserRole.SuperAdmin));

    public bool IsAcademicStaff => Roles.Contains(nameof(UserRole.Admin_AcademicStaff));

    public bool IsAdmin => Roles.Any(r => r == nameof(UserRole.SuperAdmin)
        || r == nameof(UserRole.Admin_Bachelor)
        || r == nameof(UserRole.Admin_Masters)
        || r == nameof(UserRole.Admin_PhD)
        || r == nameof(UserRole.Admin_Diploma)
        || r == nameof(UserRole.Admin_AcademicStaff));

    public StudentType? AdminStudentType => Roles.FirstOrDefault(r => r.StartsWith("Admin_")) switch
    {
        "Admin_Bachelor" => StudentType.Bachelor,
        "Admin_Masters" => StudentType.Masters,
        "Admin_PhD" => StudentType.PhD,
        "Admin_Diploma" => StudentType.Diploma,
        _ => null
    };

    public async Task<int?> GetFacultyIdAsync()
    {
        if (_facultyLoaded)
            return _facultyId;

        _facultyLoaded = true;
        var userId = UserId;
        if (userId is null)
            return _facultyId = null;

        var user = await _unitOfWork.GetRepository<User, int>().GetByIdAsync(userId.Value);
        _facultyId = user?.FacultyId;
        return _facultyId;
    }

    public async Task EnsureAdminHasFacultyAsync()
    {
        if (!IsAdmin)
            return;

        var facultyId = await GetFacultyIdAsync();
        if (facultyId is null)
            throw new ForbiddenException("Admin is not associated with a faculty.");
    }

    public async Task EnsureCanAccessFacultyAsync(int? resourceFacultyId)
    {
        if (!IsAdmin)
            return;

        var facultyId = await GetFacultyIdAsync();
        if (facultyId is null)
            throw new ForbiddenException("Admin is not associated with a faculty.");

        if (resourceFacultyId is null || resourceFacultyId != facultyId)
            throw new ForbiddenException("You can only manage resources within your own faculty.");
    }

    public async Task EnsureCanAccessByUserFacultyAsync(int userId)
    {
        if (!IsAdmin)
            return;

        var user = await _unitOfWork.GetRepository<User, int>().GetByIdAsync(userId);
        await EnsureCanAccessFacultyAsync(user?.FacultyId);
    }

    public async Task EnsureCanAccessCourseAsync(int courseId)
    {
        if (!IsAdmin)
            return;

        var course = await _unitOfWork.GetRepository<Course, int>().GetByIdAsync(courseId);
        if (course is null)
            return;

        var department = course.DepartmentId.HasValue
            ? await _unitOfWork.GetRepository<Department, int>().GetByIdAsync(course.DepartmentId.Value)
            : null;

        await EnsureCanAccessFacultyAsync(department?.FacultyId);
    }

    public async Task EnsureCanAccessExamAsync(int examId)
    {
        if (!IsAdmin)
            return;

        var exam = await _unitOfWork.GetRepository<Exam, int>().GetByIdAsync(examId);
        if (exam is null)
            return;

        await EnsureCanAccessCourseAsync(exam.CourseId);
    }
}
