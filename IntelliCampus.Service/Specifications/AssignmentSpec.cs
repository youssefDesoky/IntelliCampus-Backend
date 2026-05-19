using IntelliCampus.Domain.Entities;

namespace IntelliCampus.Service.Specifications;

public class AssignmentSpec : BaseSpecifications<Assignment>
{
    // GetByIdAsync
    public AssignmentSpec(int assignmentId)
        : base(a => a.AssignmentId == assignmentId)
    {
        AddInclude("Class.Course");
    }

    // GetByClassIdAsync
    public AssignmentSpec(int classId, bool byClass)
        : base(a => a.ClassId == classId)
    {
        AddInclude("Class.Course");
        AddOrderByDescending(a => a.DueDate);
    }
}
