using IntelliCampus.Domain.Entities;

namespace IntelliCampus.Service.Specifications;

public class AssignmentSpec : BaseSpecifications<Assignment>
{
    // GetByIdAsync
    public AssignmentSpec(int assignmentId)
        : base(a => a.AssignmentId == assignmentId)
    {
        AddInclude(a => a.Attachments);
        AddInclude(a => a.Course);
    }

    // GetByCourseIdAsync
    public AssignmentSpec(int courseId, bool byCourse)
        : base(a => a.CourseId == courseId)
    {
        AddInclude(a => a.Attachments);
        AddInclude(a => a.Course);
        AddOrderByDescending(a => a.DueDate);
    }
}
