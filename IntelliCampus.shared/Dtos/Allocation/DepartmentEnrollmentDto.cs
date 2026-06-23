namespace IntelliCampus.Shared.Dtos.Allocation;

public class DepartmentEnrollmentDto
{
    public int DepartmentId { get; set; }
    public string Name { get; set; } = null!;
    public int Enrolled { get; set; }
    public int MaxCapacity { get; set; }
}
