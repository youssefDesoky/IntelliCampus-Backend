using IntelliCampus.Domain.Entities;

namespace IntelliCampus.Service.Specifications;

public class QuizSpec : BaseSpecifications<Quiz>
{
    public QuizSpec(int quizId)
        : base(q => q.QuizId == quizId)
    {
        AddInclude(q => q.Course);
    }

    public QuizSpec(int courseId, bool byCourse)
        : base(q => q.CourseId == courseId)
    {
        AddInclude(q => q.Course);
        AddOrderByDescending(q => q.DueDate);
    }
}
