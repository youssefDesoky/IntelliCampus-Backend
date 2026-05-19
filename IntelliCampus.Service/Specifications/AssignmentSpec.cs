using IntelliCampus.Domain.Entities;

namespace IntelliCampus.Service.Specifications;

public class AssignmentSpec : BaseSpecifications<Assignment>
{
    // GetByIdAsync
    public AssignmentSpec(int assignmentId)
        : base(a => a.AssignmentId == assignmentId)
    {
        AddInclude(a => a.Attachments);
        AddInclude("Class.Course");
    }

    // GetByCourseIdAsync (via Class.CourseId)
    public AssignmentSpec(int courseId, bool byCourse)
        : base(a => a.Class.CourseId == courseId)
    {
        AddInclude(a => a.Attachments);
        AddInclude("Class.Course");
        AddOrderByDescending(a => a.DueDate);
    }
}
