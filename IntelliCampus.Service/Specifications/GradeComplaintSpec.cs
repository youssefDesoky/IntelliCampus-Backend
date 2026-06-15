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

    // GetByGradeIdAsync — instructor views complaints
    public GradeComplaintSpec(int gradeId, bool byGrade)
        : base(c => c.GradeId == gradeId)
    {
        AddInclude(c => c.Student!);
        AddInclude(c => c.Grade!);
    }
}
