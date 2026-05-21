using IntelliCampus.Domain.Entities;

namespace IntelliCampus.Service.Specifications;

internal class QuestionsByQuizSpec : BaseSpecifications<Question>
{
    public QuestionsByQuizSpec(int quizId)
        : base(q => q.QuizId == quizId) { }
}
