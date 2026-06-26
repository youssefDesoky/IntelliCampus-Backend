namespace IntelliCampus.Shared.Dtos.ElectiveBucket;

public class CreateElectiveBucketDto
{
    public string Name { get; set; } = null!;
    public string? NameAr { get; set; }
    public int BylawId { get; set; }
    public int? DepartmentId { get; set; }
    public decimal RequiredCreditHours { get; set; }
    public List<int> CourseIds { get; set; } = new();
}
