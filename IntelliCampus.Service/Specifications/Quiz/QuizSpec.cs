using IntelliCampus.Domain.Entities;
using IntelliCampus.Shared.Params;

namespace IntelliCampus.Service.Specifications;

public class QuizSpec : BaseSpecifications<Quiz>
{
    public QuizSpec(int quizId)
        : base(q => q.QuizId == quizId)
    {
        AddInclude(q => q.Course!);
    }

    public QuizSpec(int courseId, bool byCourse)
        : base(q => q.CourseId == courseId)
    {
        AddInclude(q => q.Course!);
        AddOrderByDescending(q => q.DueDate);
    }

    public QuizSpec(QuizQueryParams queryParams, int courseId)
        : base(q => q.CourseId == courseId
            && (!queryParams.QuizId.HasValue || q.QuizId == queryParams.QuizId.Value))
    {
        AddInclude(q => q.Course!);
        AddOrderByDescending(q => q.DueDate);
    }
}
