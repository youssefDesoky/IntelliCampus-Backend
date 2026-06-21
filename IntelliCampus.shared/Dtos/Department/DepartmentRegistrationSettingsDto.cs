namespace IntelliCampus.Shared.Dtos.Department;

public class DepartmentRegistrationSettingsDto
{
    public DateTime? RegistrationStartDate { get; set; }
    public DateTime? RegistrationEndDate { get; set; }
    public List<int>? AllowedLevels { get; set; }
}
