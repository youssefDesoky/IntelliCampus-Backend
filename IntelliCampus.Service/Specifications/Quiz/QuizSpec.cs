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

    // GetByCourseIdsAsync (batch - no includes, used for transcript scoring)
    public QuizSpec(List<int> courseIds)
        : base(q => courseIds.Contains(q.CourseId)) { }

    // GetByIdsAsync (batch - no includes, used for grade history)
    public QuizSpec(List<int> quizIds, bool byIds)
        : base(q => quizIds.Contains(q.QuizId)) { }

}