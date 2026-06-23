namespace IntelliCampus.Shared.Dtos.Allocation;

public class AllocationResultDto
{
    public List<StudentAllocationDto> Allocations { get; set; } = [];
    public List<UnallocatedStudentDto> Unallocated { get; set; } = [];
    public AllocationSummaryDto Summary { get; set; } = new();
}
