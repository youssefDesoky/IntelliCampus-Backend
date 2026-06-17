namespace IntelliCampus.Shared.Dtos.ElectiveBucket;

public class ElectiveBucketDto
{
    public int ElectiveBucketId { get; set; }
    public string Name { get; set; } = null!;
    public int BylawId { get; set; }
    public string? BylawName { get; set; }
    public decimal RequiredCreditHours { get; set; }
    public int? RequiredCourseCount { get; set; }
    public bool IsActive { get; set; }
    public List<ElectiveBucketCourseDto> Courses { get; set; } = new();
}

public class ElectiveBucketCourseDto
{
    public int CourseId { get; set; }
    public string? CourseCode { get; set; }
    public string CourseName { get; set; } = null!;
    public int CreditHours { get; set; }
}
