namespace IntelliCampus.Shared.Dtos.Specialization;

public class SpecializationDto
{
    public int SpecializationId { get; set; }
    public string Name { get; set; } = null!;
    public string? NameAr { get; set; }
    public int DepartmentId { get; set; }
    public string? DepartmentName { get; set; }
    public int? MaxCapacity { get; set; }
}
