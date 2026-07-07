using IntelliCampus.Domain.Entities;
using IntelliCampus.Domain.Helpers;
using IntelliCampus.Domain.Interfaces;
using IntelliCampus.Service.Specifications;
using IntelliCampus.Service_Abstraction;
using IntelliCampus.Shared.Dtos.DepartmentPreference;

namespace IntelliCampus.Service;

public class DepartmentPreferenceService : IDepartmentPreferenceService
{
    private readonly IUnitOfWork _unitOfWork;

    public DepartmentPreferenceService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    private IGenericRepository<DepartmentPreference, int> Preferences
        => _unitOfWork.GetRepository<DepartmentPreference, int>();

    private IGenericRepository<Student, int> Students
        => _unitOfWork.GetRepository<Student, int>();

    private IGenericRepository<Bylaw, int> Bylaws
        => _unitOfWork.GetRepository<Bylaw, int>();

    private IGenericRepository<StudentCourse, (int, int)> StudentCourses
        => _unitOfWork.GetRepository<StudentCourse, (int, int)>();

    private IGenericRepository<Department, int> Departments
        => _unitOfWork.GetRepository<Department, int>();

    public async Task<DepartmentPreferenceEligibilityDto> GetEligibilityAsync(int studentId)
    {
        var student = await Students.GetByIdAsync(studentId);
        if (student?.BylawId is null)
            return NoBylawResult();

        var bylaw = await Bylaws.GetByIdAsync(student.BylawId.Value);
        if (bylaw is null)
            return NoBylawResult();

        var minRequired = bylaw.Settings.MinHoursToChooseDepartment ?? 0;

        var spec = new StudentCompletedCoursesSpec(studentId);
        var completedCourses = await StudentCourses.GetAllAsync(spec, asNoTracking: true);
        var passedHours = completedCourses.Sum(sc => sc.Course.CreditHours);

        return new DepartmentPreferenceEligibilityDto
        {
            Eligible = minRequired == 0 || passedHours >= minRequired,
            PassedHours = passedHours,
            MinRequired = minRequired
        };
    }

    public async Task<DepartmentPreferenceDto> GetPreferencesAsync(int studentId)
    {
        var items = await Preferences.GetAllAsync(
            new DepartmentPreferenceByStudentSpec(studentId), asNoTracking: true);

        if (!items.Any())
            return new DepartmentPreferenceDto { Items = [] };

        var ordered = items.OrderBy(p => p.Rank).ToList();
        var departmentIds = ordered.Select(i => i.DepartmentId).Distinct().ToList();

        var departments = await Departments.GetAllAsync(
            new DepartmentByIdsSpec(departmentIds), asNoTracking: true);
        var nameMap = departments.ToDictionary(d => d.DepartmentId, d => d.DepartmentName);

        return new DepartmentPreferenceDto
        {
            Items = ordered.Select(item => new DepartmentPreferenceItemDto
            {
                DepartmentId = item.DepartmentId,
                Rank = item.Rank,
                Name = nameMap.GetValueOrDefault(item.DepartmentId)
            }).ToList()
        };
    }

    public async Task SavePreferencesAsync(int studentId, SaveDepartmentPreferenceDto dto)
    {
        var existing = await Preferences.GetAllAsync(
            new DepartmentPreferenceByStudentSpec(studentId));
        foreach (var item in existing)
            Preferences.Delete(item);

        var now = EgyptTime.Now;
        foreach (var item in dto.Items)
        {
            Preferences.Add(new DepartmentPreference
            {
                StudentId = studentId,
                DepartmentId = item.DepartmentId,
                Rank = item.Rank,
                CreatedAt = now
            });
        }

        await _unitOfWork.SaveChangesAsync();
    }

    private static DepartmentPreferenceEligibilityDto NoBylawResult()
    {
        return new DepartmentPreferenceEligibilityDto
        {
            Eligible = false,
            PassedHours = 0,
            MinRequired = 0
        };
    }
}
