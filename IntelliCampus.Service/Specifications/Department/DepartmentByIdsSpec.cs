using IntelliCampus.Domain.Entities;

namespace IntelliCampus.Service.Specifications;

internal sealed class DepartmentByIdsSpec : BaseSpecifications<Department>
{
    public DepartmentByIdsSpec(List<int> departmentIds)
        : base(d => departmentIds.Contains(d.DepartmentId)) { }
}
