namespace IntelliCampus.Domain.Entities;

public class ElectiveBucket
{
    public int ElectiveBucketId { get; set; }
    public string Name { get; set; } = null!;
    public string? NameAr { get; set; }
    public int BylawId { get; set; }
    public int? DepartmentId { get; set; }
    public decimal RequiredCreditHours { get; set; }
    public bool IsActive { get; set; } = true;

    public Bylaw Bylaw { get; set; } = null!;
    public Department? Department { get; set; }
    public ICollection<ElectiveBucketCourse> ElectiveBucketCourses { get; set; } = new List<ElectiveBucketCourse>();
    public ICollection<StudentElectiveBucketProgress> StudentProgresses { get; set; } = new List<StudentElectiveBucketProgress>();
}
