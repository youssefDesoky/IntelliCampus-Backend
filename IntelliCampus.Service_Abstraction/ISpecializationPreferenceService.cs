using IntelliCampus.Shared.Dtos.SpecializationPreference;

namespace IntelliCampus.Service_Abstraction;

public interface ISpecializationPreferenceService
{
    Task<SpecializationPreferenceEligibilityDto> GetEligibilityAsync(int studentId);
    Task<SpecializationPreferenceDto> GetPreferencesAsync(int studentId);
    Task SavePreferencesAsync(int studentId, SaveSpecializationPreferenceDto dto);
}
