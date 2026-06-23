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

    private IGenericRepository<StudentCourse, int> StudentCourses
        => _unitOfWork.GetRepository<StudentCourse, int>();

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
        var completedCourses = await StudentCourses.GetAllAsync(spec);
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
            new SpecializationPreferenceByStudentSpec(studentId));

        if (!items.Any())
            return new SpecializationPreferenceDto
            {
                TargetType = "Department",
                Items = []
            };

        var ordered = items.OrderBy(p => p.Rank).ToList();
        var targetType = ordered.First().TargetType;

        var resultItems = new List<SpecializationPreferenceItemDto>();
        foreach (var item in ordered)
        {
            string? name = null;
            if (targetType == "Department")
            {
                var dept = await Departments.GetByIdAsync(item.TargetId);
                name = dept?.DepartmentName;
            }
            else
            {
                var spec = await Specializations.GetByIdAsync(item.TargetId);
                name = spec?.Name;
            }

            resultItems.Add(new SpecializationPreferenceItemDto
            {
                TargetId = item.TargetId,
                Rank = item.Rank,
                Name = name
            });
        }

        return new SpecializationPreferenceDto
        {
            TargetType = targetType,
            Items = resultItems
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
