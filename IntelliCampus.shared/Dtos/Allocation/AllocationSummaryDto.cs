namespace IntelliCampus.Shared.Dtos.Allocation;

public class AllocationSummaryDto
{
    public List<SpecializationEnrollmentDto> Specializations { get; set; } = [];
    public List<DepartmentEnrollmentDto> Departments { get; set; } = [];
}
