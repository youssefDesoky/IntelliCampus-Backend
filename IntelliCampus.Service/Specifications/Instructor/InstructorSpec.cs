using IntelliCampus.Domain.Entities;
using IntelliCampus.Domain.Entities.Enums;
using IntelliCampus.Shared.Params;

namespace IntelliCampus.Service.Specifications
{
    internal class InstructorSpec : BaseSpecifications<Instructor>
    {
    internal static System.Linq.Expressions.Expression<Func<Instructor, bool>> BuildPredicate(InstructorQueryParams queryParams)
    {
        InstructorRole? parsedRole = null;
        if (!string.IsNullOrEmpty(queryParams.InstructorRole) && Enum.TryParse<InstructorRole>(queryParams.InstructorRole, ignoreCase: true, out var ir))
            parsedRole = ir;

        return i =>
            (!queryParams.DepartmentId.HasValue || i.DepartmentId == queryParams.DepartmentId.Value) &&
            (!queryParams.FacultyId.HasValue || i.User.FacultyId == queryParams.FacultyId.Value) &&
            (!parsedRole.HasValue || (i.InstructorRole.HasValue && i.InstructorRole.Value == parsedRole.Value)) &&
            (string.IsNullOrEmpty(queryParams.Search) || i.User.FullName.Contains(queryParams.Search) || (i.InstructorCode != null && i.InstructorCode.Contains(queryParams.Search)));
    }

    public InstructorSpec()
    {
        AddInclude(i => i.Department!);
        AddInclude(i => i.OfficeHoursRoom!);
        AddInclude(i => i.User.Faculty!);
        AddInclude("User.UserRoles.Role");
        EnableSplitQuery();
    }

    public InstructorSpec(params InstructorRole[] roles)
        : base(i => i.InstructorRole != null && roles.Contains(i.InstructorRole.Value))
    {
        AddInclude(i => i.Department!);
        AddInclude(i => i.OfficeHoursRoom!);
        AddInclude("User.UserRoles.Role");
        EnableSplitQuery();
    }

    public InstructorSpec(int instructorId)
        : base(i => i.UserId == instructorId)
    {
        AddInclude(i => i.Department!);
        AddInclude(i => i.OfficeHoursRoom!);
        AddInclude(i => i.User.Faculty!);
        AddInclude("User.UserRoles.Role");
        EnableSplitQuery();
    }

    public InstructorSpec(InstructorRole[] roles, ClassQueryParams queryParams)
        : base(i => i.InstructorRole != null && roles.Contains(i.InstructorRole.Value))
    {
        AddInclude(i => i.Department!);
        AddInclude(i => i.OfficeHoursRoom!);
        AddInclude("User.UserRoles.Role");
        EnableSplitQuery();
        AddOrderBy(i => i.User.FullName);
        ApplyPagination(queryParams.PageSize, queryParams.PageIndex);
    }

    public InstructorSpec(InstructorQueryParams queryParams)
        : base(BuildPredicate(queryParams))
    {
        AddInclude(i => i.Department!);
        AddInclude(i => i.OfficeHoursRoom!);
        AddInclude(i => i.User.Faculty!);
        AddInclude("User.UserRoles.Role");
        EnableSplitQuery();
        AddOrderBy(i => i.User.FullName);
        ApplyPagination(queryParams.PageSize, queryParams.PageIndex);
    }
}
}
