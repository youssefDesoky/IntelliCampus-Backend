using IntelliCampus.Domain.Entities;
using IntelliCampus.Domain.Entities.Enums;
using IntelliCampus.Shared.Params;

namespace IntelliCampus.Service.Specifications
{
    internal class InstructorSpec : BaseSpecifications<Instructor>
    {
    public InstructorSpec()
    {
        AddInclude(i => i.Department!);
        AddInclude(i => i.OfficeHoursRoom!);
        AddInclude("UserRoles.Role");
        EnableSplitQuery();
    }

    public InstructorSpec(params InstructorRole[] roles)
        : base(i => i.InstructorRole != null && roles.Contains(i.InstructorRole.Value))
    {
        AddInclude(i => i.Department!);
        AddInclude(i => i.OfficeHoursRoom!);
        AddInclude("UserRoles.Role");
        EnableSplitQuery();
    }

    public InstructorSpec(int instructorId)
        : base(i => i.UserId == instructorId)
    {
        AddInclude(i => i.Department!);
        AddInclude(i => i.OfficeHoursRoom!);
        AddInclude("UserRoles.Role");
        EnableSplitQuery();
    }

    public InstructorSpec(InstructorRole[] roles, ClassQueryParams queryParams)
        : base(i => i.InstructorRole != null && roles.Contains(i.InstructorRole.Value))
    {
        AddInclude(i => i.Department!);
        AddInclude(i => i.OfficeHoursRoom!);
        AddInclude("UserRoles.Role");
        EnableSplitQuery();
        ApplyPagination(queryParams.PageSize, queryParams.PageIndex);
    }

    public InstructorSpec(InstructorQueryParams queryParams)
        : base(i =>
            (!queryParams.DepartmentId.HasValue || i.DepartmentId == queryParams.DepartmentId.Value) &&
            (!queryParams.FacultyId.HasValue || i.FacultyId == queryParams.FacultyId.Value))
    {
        AddInclude(i => i.Department!);
        AddInclude(i => i.OfficeHoursRoom!);
        AddInclude("UserRoles.Role");
        EnableSplitQuery();
        ApplyPagination(queryParams.PageSize, queryParams.PageIndex);
    }
    }
}
