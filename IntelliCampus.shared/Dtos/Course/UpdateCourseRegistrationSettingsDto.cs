namespace IntelliCampus.Shared.Dtos.Course;

public class UpdateCourseRegistrationSettingsDto
{
    public string? RegStartDate { get; set; }
    public string? RegEndDate { get; set; }
    public List<int> AllowedLevels { get; set; } = [];
    public List<int> AllowedDepartmentIds { get; set; } = [];
}
