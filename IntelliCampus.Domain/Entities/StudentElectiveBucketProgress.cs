namespace IntelliCampus.Domain.Entities;

public class StudentElectiveBucketProgress
{
    public int StudentId { get; set; }
    public int ElectiveBucketId { get; set; }
    public decimal CompletedCreditHours { get; set; }
    public int CompletedCourseCount { get; set; }
    public bool IsLocked { get; set; }

    public Student Student { get; set; } = null!;
    public ElectiveBucket ElectiveBucket { get; set; } = null!;
}
