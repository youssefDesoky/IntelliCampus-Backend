using IntelliCampus.Domain.Entities;
using IntelliCampus.Shared.Params;

namespace IntelliCampus.Service.Specifications;

public class AssignmentSpec : BaseSpecifications<Assignment>
{
    // GetByIdAsync
    public AssignmentSpec(int assignmentId)
        : base(a => a.AssignmentId == assignmentId)
    {
        AddInclude(a => a.Attachments!);
        EnableSplitQuery();
    }
// GetByCourseIdAsync
    public AssignmentSpec(int courseId, bool byCourse)
        : base(a => a.CourseId == courseId)
    {
        AddInclude(a => a.Attachments!);
        EnableSplitQuery();
        AddOrderByDescending(a => a.DueDate);
    }

    // GetByCourseIdAsync with pagination
    public AssignmentSpec(int courseId, bool byCourse, AssignmentQueryParams queryParams)
        : base(a => a.CourseId == courseId)
    {
        AddInclude(a => a.Attachments!);
        EnableSplitQuery();
        AddOrderByDescending(a => a.DueDate);
        ApplyPagination(queryParams.PageSize, queryParams.PageIndex);
    }

    // GetByCourseIdsAsync (batch - no includes, used for transcript scoring)
    public AssignmentSpec(List<int> courseIds)
        : base(a => courseIds.Contains(a.CourseId)) { }

    // GetByIdsAsync (batch - no includes, used for grade history)
    public AssignmentSpec(List<int> assignmentIds, bool byIds)
        : base(a => assignmentIds.Contains(a.AssignmentId)) { }

}