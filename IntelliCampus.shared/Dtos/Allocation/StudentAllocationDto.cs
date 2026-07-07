namespace IntelliCampus.Shared.Dtos.Allocation;

public class StudentAllocationDto
{
    public int StudentId { get; set; }
    public string StudentName { get; set; } = null!;
    public int DepartmentId { get; set; }
    public string DepartmentName { get; set; } = null!;
}
