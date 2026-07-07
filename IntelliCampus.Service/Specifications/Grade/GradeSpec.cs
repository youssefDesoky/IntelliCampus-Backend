using IntelliCampus.Domain.Entities;

namespace IntelliCampus.Service.Specifications;

public class GradeSpec : BaseSpecifications<Grade>
{
    // GetByStudentIdAsync
    public GradeSpec(int studentId)
        : base(g => g.StudentId == studentId)
    {
        AddInclude(g => g.Course!);
        AddOrderBy(g => g.GradedAt);
    }

    // GetBreakdownAsync — student + course
    public GradeSpec(int studentId, int courseId)
        : base(g => g.StudentId == studentId && g.CourseId == courseId)
    {
        AddInclude(g => g.Course!);
        AddOrderBy(g => g.GradedAt);
    }

    // GetByCourseAsync — all grades for a course (instructor view), no includes
    public GradeSpec(int courseId, bool byCourse)
        : base(g => g.CourseId == courseId) { }

    // GetByIdsAsync — all grades for a set of grade PKs, no includes
    public GradeSpec(ICollection<int> gradeIds, bool byIds)
        : base(g => gradeIds.Contains(g.GradeId)) { }

    // Admin dashboard — grades within a date range
    public GradeSpec(DateTime from, DateTime to)
        : base(g => g.GradedAt >= from && g.GradedAt <= to)
    {
        AddInclude("Student.User");
    }
}
