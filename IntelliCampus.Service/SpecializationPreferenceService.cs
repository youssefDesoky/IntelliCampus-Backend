using IntelliCampus.Domain.Entities;
using IntelliCampus.Domain.Entities.Enums;
using IntelliCampus.Domain.Helpers;
using IntelliCampus.Domain.Interfaces;
using IntelliCampus.Service.Exceptions;
using IntelliCampus.Service.Specifications;
using IntelliCampus.Service_Abstraction;
using IntelliCampus.Shared.Dtos.SpecializationPreference;

namespace IntelliCampus.Service;

public class SpecializationPreferenceService : ISpecializationPreferenceService
{
    private readonly IUnitOfWork _unitOfWork;

    public SpecializationPreferenceService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    private IGenericRepository<SpecializationPreference, int> Preferences
        => _unitOfWork.GetRepository<SpecializationPreference, int>();

    private IGenericRepository<Student, int> Students
        => _unitOfWork.GetRepository<Student, int>();

    private IGenericRepository<Bylaw, int> Bylaws
        => _unitOfWork.GetRepository<Bylaw, int>();

    private IGenericRepository<StudentCourse, (int, int)> StudentCourses
        => _unitOfWork.GetRepository<StudentCourse, (int, int)>();

    private IGenericRepository<Department, int> Departments
        => _unitOfWork.GetRepository<Department, int>();

    private IGenericRepository<Specialization, int> Specializations
        => _unitOfWork.GetRepository<Specialization, int>();

    public async Task<SpecializationPreferenceEligibilityDto> GetEligibilityAsync(int studentId)
    {
        var student = await Students.GetByIdAsync(studentId);
        if (student?.BylawId is null)
            return NoBylawResult();

        var bylaw = await Bylaws.GetByIdAsync(student.BylawId.Value);
        if (bylaw is null)
            return NoBylawResult();

        var targetType = bylaw.Settings.MinHoursToChooseSpecialization.HasValue
            ? "Specialization"
            : "Department";

        var minRequired = targetType == "Specialization"
            ? bylaw.Settings.MinHoursToChooseSpecialization ?? 0
            : bylaw.Settings.MinHoursToChooseDepartment ?? 0;

        var spec = new StudentCompletedCoursesSpec(studentId);
        var completedCourses = await StudentCourses.GetAllAsync(spec, asNoTracking: true);
        var passedHours = completedCourses.Sum(sc => sc.Course.CreditHours);

        return new SpecializationPreferenceEligibilityDto
        {
            TargetType = targetType,
            Eligible = minRequired == 0 || passedHours >= minRequired,
            PassedHours = passedHours,
            MinRequired = minRequired
        };
    }

    public async Task<SpecializationPreferenceDto> GetPreferencesAsync(int studentId)
    {
        var items = await Preferences.GetAllAsync(
            new SpecializationPreferenceByStudentSpec(studentId), asNoTracking: true);

        if (!items.Any())
            return new SpecializationPreferenceDto
            {
                TargetType = "Department",
                Items = []
            };

        var ordered = items.OrderBy(p => p.Rank).ToList();
        var targetType = ordered.FirstOrDefault()?.TargetType;

        var targetIds = ordered.Select(i => i.TargetId).Distinct().ToList();
        Dictionary<int, string> nameMap;
        if (targetType == "Department")
        {
            var departments = await Departments.GetAllAsync(new DepartmentByIdsSpec(targetIds), asNoTracking: true);
            nameMap = departments.ToDictionary(d => d.DepartmentId, d => d.DepartmentName);
        }
        else
        {
            var specializations = await Specializations.GetAllAsync(new SpecializationByIdsSpec(targetIds), asNoTracking: true);
            nameMap = specializations.ToDictionary(s => s.SpecializationId, s => s.Name);
        }

        return new SpecializationPreferenceDto
        {
            TargetType = targetType,
            Items = ordered.Select(item => new SpecializationPreferenceItemDto
            {
                TargetId = item.TargetId,
                Rank = item.Rank,
                Name = nameMap.GetValueOrDefault(item.TargetId)
            }).ToList()
        };
    }

    public async Task SavePreferencesAsync(int studentId, SaveSpecializationPreferenceDto dto)
    {
        if (dto.TargetType != "Department" && dto.TargetType != "Specialization")
            throw new ArgumentException("TargetType must be 'Department' or 'Specialization'.");

        var existing = await Preferences.GetAllAsync(
            new SpecializationPreferenceByStudentSpec(studentId));
        foreach (var item in existing)
            Preferences.Delete(item);

        var now = EgyptTime.Now;
        foreach (var item in dto.Items)
        {
            Preferences.Add(new SpecializationPreference
            {
                StudentId = studentId,
                TargetType = dto.TargetType,
                TargetId = item.TargetId,
                Rank = item.Rank,
                CreatedAt = now
            });
        }

        await _unitOfWork.SaveChangesAsync();
    }

    private static SpecializationPreferenceEligibilityDto NoBylawResult()
    {
        return new SpecializationPreferenceEligibilityDto
        {
            TargetType = "Department",
            Eligible = false,
            PassedHours = 0,
            MinRequired = 0
        };
    }
}
