using IntelliCampus.Domain.Entities;

namespace IntelliCampus.Service.Specifications;

public class GradeSpec : BaseSpecifications<Grade>
{
    // GetByStudentIdAsync
    public GradeSpec(int studentId)
        : base(g => g.StudentId == studentId)
    {
        AddInclude(g => g.Course);
        AddOrderBy(g => g.GradedAt);
    }

    // GetBreakdownAsync — student + course
    public GradeSpec(int studentId, int courseId)
        : base(g => g.StudentId == studentId && g.CourseId == courseId)
    {
        AddInclude(g => g.Course);
        AddOrderBy(g => g.GradedAt);
    }
}
