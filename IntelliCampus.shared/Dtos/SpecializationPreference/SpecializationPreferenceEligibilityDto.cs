namespace IntelliCampus.Shared.Dtos.SpecializationPreference;

public class SpecializationPreferenceEligibilityDto
{
    public string TargetType { get; set; } = null!;
    public bool Eligible { get; set; }
    public int PassedHours { get; set; }
    public int MinRequired { get; set; }
}
