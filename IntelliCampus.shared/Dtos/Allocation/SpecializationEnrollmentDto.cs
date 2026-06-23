namespace IntelliCampus.Shared.Dtos.Allocation;

public class SpecializationEnrollmentDto
{
    public int SpecializationId { get; set; }
    public string Name { get; set; } = null!;
    public int Enrolled { get; set; }
    public int MaxCapacity { get; set; }
}
