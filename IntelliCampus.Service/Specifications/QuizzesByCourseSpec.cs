using IntelliCampus.Domain.Entities;

namespace IntelliCampus.Service.Specifications;

internal class QuizzesByCourseSpec : BaseSpecifications<Quiz>
{
    public QuizzesByCourseSpec(int courseId)
        : base(q => q.Class!.CourseId == courseId)
    {
        AddInclude("Class.Course");
    }
}
