namespace IntelliCampus.Shared.Dtos.SpecializationPreference;

public class SpecializationPreferenceDto
{
    public string TargetType { get; set; } = null!;
    public List<SpecializationPreferenceItemDto> Items { get; set; } = [];
}
