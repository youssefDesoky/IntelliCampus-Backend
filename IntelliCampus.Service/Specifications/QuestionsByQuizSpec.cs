using IntelliCampus.Domain.Entities;

namespace IntelliCampus.Service.Specifications
{
    public class QuestionsByQuizSpec : BaseSpecifications<Question>
    {
        public QuestionsByQuizSpec(int quizId)
            : base(q => q.QuizId == quizId)
        {
        }
    }
}
