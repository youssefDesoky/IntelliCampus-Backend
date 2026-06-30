using IntelliCampus.Domain.Entities;
using IntelliCampus.Domain.Entities.Enums;
using IntelliCampus.Shared.Params;

namespace IntelliCampus.Service.Specifications;

internal class ProfessorsSpec : BaseSpecifications<Instructor>
{
    public ProfessorsSpec(int? departmentId = null, int? facultyId = null)
        : base(i => i.InstructorRole != null &&
            (i.InstructorRole == InstructorRole.Professor ||
             i.InstructorRole == InstructorRole.Lecturer ||
             i.InstructorRole == InstructorRole.AssociateProfessor)
            && (!departmentId.HasValue || i.DepartmentId == departmentId.Value)
            && (!facultyId.HasValue || i.User.FacultyId == facultyId.Value))
    {
        AddInclude(i => i.Department!);
        AddInclude(i => i.OfficeHoursRoom!);
        AddInclude("User.UserRoles.Role");
        EnableSplitQuery();
    }

    public ProfessorsSpec(InstructorQueryParams queryParams)
        : base(i => i.InstructorRole != null &&
            (i.InstructorRole == InstructorRole.Professor ||
             i.InstructorRole == InstructorRole.Lecturer ||
             i.InstructorRole == InstructorRole.AssociateProfessor)
            && (!queryParams.DepartmentId.HasValue || i.DepartmentId == queryParams.DepartmentId.Value)
            && (!queryParams.FacultyId.HasValue || i.User.FacultyId == queryParams.FacultyId.Value))
    {
        AddInclude(i => i.Department!);
        AddInclude(i => i.OfficeHoursRoom!);
        AddInclude("User.UserRoles.Role");
        EnableSplitQuery();
    }
}
