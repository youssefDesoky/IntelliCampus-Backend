using IntelliCampus.Domain.Entities;
using IntelliCampus.Shared.Params;

namespace IntelliCampus.Service.Specifications;

internal class InstructorCountSpec : BaseSpecifications<Instructor>
{
    public InstructorCountSpec(InstructorQueryParams queryParams)
        : base(i =>
            (!queryParams.DepartmentId.HasValue || i.DepartmentId == queryParams.DepartmentId.Value) &&
            (!queryParams.FacultyId.HasValue || i.FacultyId == queryParams.FacultyId.Value))
    {
    }
}