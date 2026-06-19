using IntelliCampus.Domain.Entities;
using IntelliCampus.Domain.Entities.Enums;

namespace IntelliCampus.Service.Specifications;

internal class ProfessorsSpec : BaseSpecifications<Instructor>
{
    public ProfessorsSpec()
        : base(i => i.InstructorRole != null &&
            (i.InstructorRole == InstructorRole.Professor ||
             i.InstructorRole == InstructorRole.Lecturer ||
             i.InstructorRole == InstructorRole.AssociateProfessor))
    {
        AddInclude(i => i.Department!);
        AddInclude(i => i.OfficeHoursRoom!);
        AddInclude("UserRoles.Role");
    }
}
