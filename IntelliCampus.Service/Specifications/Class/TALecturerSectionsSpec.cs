using IntelliCampus.Domain.Entities;
using IntelliCampus.Domain.Entities.Enums;

namespace IntelliCampus.Service.Specifications;

internal class TALecturerSectionsSpec : BaseSpecifications<Class>
{
    public TALecturerSectionsSpec()
        : base(c => c.ClassType == ClassType.Section
            && c.Instructor != null
            && (c.Instructor.InstructorRole == InstructorRole.TeachingAssistant
                || c.Instructor.InstructorRole == InstructorRole.AssistantLecturer))
    {
        AddInclude(c => c.Course!);
        AddInclude(c => c.Instructor!);
    }
}
