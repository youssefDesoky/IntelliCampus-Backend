using IntelliCampus.Domain.Entities;

namespace IntelliCampus.Service.Specifications;

public sealed class ExamScheduleByStudentAndCourseSpec : BaseSpecifications<ExamSchedule>
{
    public ExamScheduleByStudentAndCourseSpec(int studentId, int courseId)
        : base(es => es.StudentId == studentId && es.CourseId == courseId)
    {
    }
}
