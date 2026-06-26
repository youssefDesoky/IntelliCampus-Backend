namespace IntelliCampus.Shared.Dtos.ElectiveBucket;

public class ElectiveBucketProgressDto
{
    public int ElectiveBucketId { get; set; }
    public string BucketName { get; set; } = null!;
    public decimal RequiredCreditHours { get; set; }
    public decimal CompletedCreditHours { get; set; }
    public decimal RemainingCreditHours { get; set; }
    public bool IsLocked { get; set; }
    public bool IsRequirementMet { get; set; }
}
