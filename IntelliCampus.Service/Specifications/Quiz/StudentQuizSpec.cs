using IntelliCampus.Domain.Entities;

namespace IntelliCampus.Service.Specifications;

public class StudentQuizSpec : BaseSpecifications<StudentQuiz>
{
    // GetResultAsync
    public StudentQuizSpec(int studentId, int quizId)
        : base(sq => sq.StudentId == studentId && sq.QuizId == quizId)
    {
        AddInclude(sq => sq.Quiz!);
        AddInclude(sq => sq.Student!);
    }

    // GetAllResultsAsync — all students for a quiz
    public StudentQuizSpec(int quizId, bool allResults)
        : base(sq => sq.QuizId == quizId)
    {
        AddInclude(sq => sq.Student!);
        AddInclude(sq => sq.Quiz!);
    }

    // GetByStudentIdAsync
    public StudentQuizSpec(int studentId, bool byStudent, bool dummy)
        : base(sq => sq.StudentId == studentId)
    {
        AddInclude(sq => sq.Quiz!);
        AddOrderByDescending(sq => sq.SubmittedAt);
    }
}
