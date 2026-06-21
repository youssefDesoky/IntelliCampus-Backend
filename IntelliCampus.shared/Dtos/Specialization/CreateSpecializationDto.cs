namespace IntelliCampus.Shared.Dtos.Specialization;

public class CreateSpecializationDto
{
    public string Name { get; set; } = null!;
    public string? NameAr { get; set; }
    public int DepartmentId { get; set; }
    public int? MaxCapacity { get; set; }
}
