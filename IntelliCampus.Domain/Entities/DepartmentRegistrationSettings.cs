namespace IntelliCampus.Domain.Entities;

public class DepartmentRegistrationSettings
{
    public DateTime? RegistrationStartDate { get; set; }
    public DateTime? RegistrationEndDate { get; set; }
    public List<int> AllowedLevels { get; set; } = new();
}
