using IntelliCampus.Domain.Entities;

namespace IntelliCampus.Service.Specifications;

public sealed class ScheduleByStudentAndCourseSpec : BaseSpecifications<Schedule>
{
    public ScheduleByStudentAndCourseSpec(int studentId, int courseId)
        : base(s => s.StudentId == studentId && s.CourseId == courseId)
    {
        AddOrderBy(s => s.Date);
    }
}
