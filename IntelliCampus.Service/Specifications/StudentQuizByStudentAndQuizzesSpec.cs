using IntelliCampus.Domain.Entities;

namespace IntelliCampus.Service.Specifications
{
    public class StudentQuizByStudentAndQuizzesSpec : BaseSpecifications<StudentQuiz>
    {
        public StudentQuizByStudentAndQuizzesSpec(int studentId, IEnumerable<int> quizIds)
            : base(sq => sq.StudentId == studentId && quizIds.Contains(sq.QuizId))
        {
        }
    }
}
