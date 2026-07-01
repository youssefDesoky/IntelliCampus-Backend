using IntelliCampus.Domain.Entities;

namespace IntelliCampus.Service.Specifications;

public class GradeComplaintSpec : BaseSpecifications<GradeComplaint>
{
    // GetByStudentIdAsync
    public GradeComplaintSpec(int studentId)
        : base(c => c.StudentId == studentId)
    {
        AddInclude(c => c.Grade!);
        AddOrderByDescending(c => c.SubmittedAt);
    }

    // GetByGradeIdAsync - instructor views complaints
    public GradeComplaintSpec(int gradeId, bool byGrade)
        : base(c => c.GradeId == gradeId)
    {
        AddInclude(c => c.Student!);
        AddInclude(c => c.Grade!);
        EnableSplitQuery();
    }

    // GetByCourseIdAsync - instructor views complaints for a course
    // Course filtering is done in-memory via BelongsToCourse since GradeId is polymorphic
    public GradeComplaintSpec(int courseId, bool byCourse, bool unused)
        : base(null)
    {
        AddInclude(c => c.Student!);
        EnableSplitQuery();
    }
}
