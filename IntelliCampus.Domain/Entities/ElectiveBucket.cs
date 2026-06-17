namespace IntelliCampus.Domain.Entities;

public class ElectiveBucket
{
    public int ElectiveBucketId { get; set; }
    public string Name { get; set; } = null!;
    public int BylawId { get; set; }
    public decimal RequiredCreditHours { get; set; }
    public int? RequiredCourseCount { get; set; }
    public bool IsActive { get; set; } = true;

    public Bylaw Bylaw { get; set; } = null!;
    public ICollection<ElectiveBucketCourse> ElectiveBucketCourses { get; set; } = new List<ElectiveBucketCourse>();
    public ICollection<StudentElectiveBucketProgress> StudentProgresses { get; set; } = new List<StudentElectiveBucketProgress>();
}
