using IntelliCampus.Domain.Entities;
using IntelliCampus.Shared.Params;

namespace IntelliCampus.Service.Specifications;

internal class DepartmentCountSpec : BaseSpecifications<Department>
{
    public DepartmentCountSpec(DepartmentQueryParams queryParams)
        : base(d =>
            (!queryParams.FacultyId.HasValue || d.FacultyId == queryParams.FacultyId.Value) &&
            (string.IsNullOrEmpty(queryParams.Search) || d.DepartmentName.Contains(queryParams.Search)))
    {
    }
}