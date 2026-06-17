namespace IntelliCampus.Shared.Dtos.ElectiveBucket;

public class ElectiveBucketProgressDto
{
    public int ElectiveBucketId { get; set; }
    public string BucketName { get; set; } = null!;
    public decimal RequiredCreditHours { get; set; }
    public int? RequiredCourseCount { get; set; }
    public decimal CompletedCreditHours { get; set; }
    public int CompletedCourseCount { get; set; }
    public decimal RemainingCreditHours { get; set; }
    public int RemainingCourseCount { get; set; }
    public bool IsLocked { get; set; }
    public bool IsRequirementMet { get; set; }
}
