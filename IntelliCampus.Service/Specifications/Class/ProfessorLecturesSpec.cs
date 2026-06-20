using IntelliCampus.Domain.Entities;
using IntelliCampus.Domain.Entities.Enums;

namespace IntelliCampus.Service.Specifications;

internal class ProfessorLecturesSpec : BaseSpecifications<Class>
{
    public ProfessorLecturesSpec()
        : base(c => c.ClassType == ClassType.Lecture
            && c.Instructor != null
            && (c.Instructor.InstructorRole == InstructorRole.Professor
                || c.Instructor.InstructorRole == InstructorRole.Lecturer
                || c.Instructor.InstructorRole == InstructorRole.AssociateProfessor))
    {
        AddInclude(c => c.Course!);
        AddInclude(c => c.Instructor!);
    }
}
