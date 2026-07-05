using IntelliCampus.Domain.Entities;
using IntelliCampus.Shared.Params;

namespace IntelliCampus.Service.Specifications;

internal class InstructorCountSpec : BaseSpecifications<Instructor>
{
    public InstructorCountSpec(InstructorQueryParams queryParams)
        : base(i =>
            (!queryParams.DepartmentId.HasValue || i.DepartmentId == queryParams.DepartmentId.Value) &&
            (!queryParams.FacultyId.HasValue || i.User.FacultyId == queryParams.FacultyId.Value) &&
            (string.IsNullOrEmpty(queryParams.Search) || i.User.FullName.Contains(queryParams.Search) || (i.InstructorCode != null && i.InstructorCode.Contains(queryParams.Search))))
    {
    }
}