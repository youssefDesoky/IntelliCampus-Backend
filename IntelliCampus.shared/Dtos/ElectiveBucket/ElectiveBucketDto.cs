namespace IntelliCampus.Shared.Dtos.ElectiveBucket;

public class ElectiveBucketDto
{
    public int ElectiveBucketId { get; set; }
    public string Name { get; set; } = null!;
    public string? NameAr { get; set; }
    public int BylawId { get; set; }
    public string? BylawName { get; set; }
    public string? BylawNameAr { get; set; }
    public int? DepartmentId { get; set; }
    public string? DepartmentName { get; set; }
    public string? DepartmentNameAr { get; set; }
    public decimal RequiredCreditHours { get; set; }
    public bool IsActive { get; set; }
    public List<ElectiveBucketCourseDto> Courses { get; set; } = new();
}

public class ElectiveBucketCourseDto
{
    public int CourseId { get; set; }
    public string? CourseCode { get; set; }
    public string? CourseCodeAr { get; set; }
    public string CourseName { get; set; } = null!;
    public string? CourseNameAr { get; set; }
    public int CreditHours { get; set; }
    public int? BylawCourseId { get; set; }
}
