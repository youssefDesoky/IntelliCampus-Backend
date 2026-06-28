namespace IntelliCampus.Shared.Dtos.Grade;

public class AcademicProgressDto
{
    public int TotalCompletedHours { get; set; }
    public int TotalRequiredHours { get; set; }
    public int TotalGraduationHours { get; set; }
    public double Gpa { get; set; }
    public List<BylawBucketDto> Buckets { get; set; } = [];
}

public class BylawBucketDto
{
    public string BucketName { get; set; } = string.Empty;
    public string BucketType { get; set; } = string.Empty;
    public int CompletedHours { get; set; }
    public int RequiredHours { get; set; }
    public List<BucketCourseDto> Courses { get; set; } = [];
}

public class BucketCourseDto
{
    public int CourseId { get; set; }
    public string CourseCode { get; set; } = string.Empty;
    public string CourseName { get; set; } = string.Empty;
    public int CreditHours { get; set; }
    public bool IsCompleted { get; set; }
}
