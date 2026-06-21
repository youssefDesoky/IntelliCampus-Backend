namespace IntelliCampus.Shared.Dtos.Course;

public class CourseRegistrationSettingsDto
{
    public string? RegistrationStartDate { get; set; }
    public string? RegistrationEndDate { get; set; }
    public List<int>? AllowedLevels { get; set; }
    public List<int>? AllowedDepartments { get; set; }
}
