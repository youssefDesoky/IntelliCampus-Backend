using IntelliCampus.Shared.Dtos.Allocation;

namespace IntelliCampus.Service_Abstraction;

public interface IDepartmentAllocationService
{
    Task<AllocationResultDto> RunAllocationAsync();
}
