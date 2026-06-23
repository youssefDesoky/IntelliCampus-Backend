namespace IntelliCampus.Shared.Dtos.Allocation;

public class UnallocatedStudentDto
{
    public int StudentId { get; set; }
    public string StudentName { get; set; } = null!;
    public string Reason { get; set; } = null!;
}
