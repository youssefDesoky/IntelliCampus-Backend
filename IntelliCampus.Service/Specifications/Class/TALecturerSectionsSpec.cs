using IntelliCampus.Domain.Entities;
using IntelliCampus.Domain.Entities.Enums;
using IntelliCampus.Shared.Params;

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
        EnableSplitQuery();
    }

    public TALecturerSectionsSpec(int instructorId)
        : base(c => c.ClassType == ClassType.Section
            && c.InstructorId == instructorId
            && c.Instructor != null
            && (c.Instructor.InstructorRole == InstructorRole.TeachingAssistant
                || c.Instructor.InstructorRole == InstructorRole.AssistantLecturer))
    {
        AddInclude(c => c.Course!);
        AddInclude(c => c.Instructor!);
        EnableSplitQuery();
    }

    public TALecturerSectionsSpec(ClassQueryParams queryParams)
        : base(c => c.ClassType == ClassType.Section
            && c.Instructor != null
            && (c.Instructor.InstructorRole == InstructorRole.TeachingAssistant
                || c.Instructor.InstructorRole == InstructorRole.AssistantLecturer)
            && (!queryParams.InstructorId.HasValue || c.InstructorId == queryParams.InstructorId.Value))
    {
        AddInclude(c => c.Course!);
        AddInclude(c => c.Instructor!);
        EnableSplitQuery();
        AddOrderBy(c => c.ClassId);
        ApplyPagination(queryParams.PageSize, queryParams.PageIndex);
    }
}
