using IntelliCampus.Shared.Dtos.Allocation;

namespace IntelliCampus.Service_Abstraction;

public interface ISpecializationAllocationService
{
    Task<AllocationResultDto> RunAllocationAsync();
}
