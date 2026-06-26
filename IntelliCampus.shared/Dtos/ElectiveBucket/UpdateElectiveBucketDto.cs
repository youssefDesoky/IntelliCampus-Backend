namespace IntelliCampus.Shared.Dtos.ElectiveBucket;

public class UpdateElectiveBucketDto
{
    public string? Name { get; set; }
    public string? NameAr { get; set; }
    public decimal? RequiredCreditHours { get; set; }
    public bool? IsActive { get; set; }
    public List<int>? CourseIds { get; set; }
}
