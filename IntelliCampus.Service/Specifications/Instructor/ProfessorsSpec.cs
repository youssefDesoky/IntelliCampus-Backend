using IntelliCampus.Domain.Entities;
using IntelliCampus.Domain.Entities.Enums;

namespace IntelliCampus.Service.Specifications;

internal class ProfessorsSpec : BaseSpecifications<Instructor>
{
    public ProfessorsSpec(int? departmentId = null, int? facultyId = null)
        : base(i => i.InstructorRole != null &&
            (i.InstructorRole == InstructorRole.Professor ||
             i.InstructorRole == InstructorRole.Lecturer ||
             i.InstructorRole == InstructorRole.AssociateProfessor)
            && (!departmentId.HasValue || i.DepartmentId == departmentId.Value)
            && (!facultyId.HasValue || i.FacultyId == facultyId.Value))
    {
        AddInclude(i => i.Department!);
        AddInclude(i => i.OfficeHoursRoom!);
        AddInclude("UserRoles.Role");
    }
}
