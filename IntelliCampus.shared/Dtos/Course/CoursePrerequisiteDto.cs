namespace IntelliCampus.Shared.Dtos.Course;

public class CoursePrerequisiteDto
{
    public int CourseId { get; set; }
    public string CourseName { get; set; } = null!;
    public string? CourseCode { get; set; }
    public int CreditHours { get; set; }
    public List<PrerequisiteItemDto> Prerequisites { get; set; } = [];
}

public class PrerequisiteItemDto
{
    public string Code { get; set; } = null!;
    public string Title { get; set; } = null!;
}
