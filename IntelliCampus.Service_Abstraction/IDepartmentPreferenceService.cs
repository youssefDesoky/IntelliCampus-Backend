using IntelliCampus.Shared.Dtos.DepartmentPreference;

namespace IntelliCampus.Service_Abstraction;

public interface IDepartmentPreferenceService
{
    Task<DepartmentPreferenceEligibilityDto> GetEligibilityAsync(int studentId);
    Task<DepartmentPreferenceDto> GetPreferencesAsync(int studentId);
    Task SavePreferencesAsync(int studentId, SaveDepartmentPreferenceDto dto);
}
