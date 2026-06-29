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
        EnableSplitQuery();
    }

    // GetAllResultsAsync — all students for a quiz
    public StudentQuizSpec(int quizId, bool allResults)
        : base(sq => sq.QuizId == quizId)
    {
        AddInclude(sq => sq.Student!);
        AddInclude(sq => sq.Quiz!);
        EnableSplitQuery();
    }

    // GetByStudentIdAsync — only submitted records
    public StudentQuizSpec(int studentId, bool byStudent, bool dummy)
        : base(sq => sq.StudentId == studentId && sq.SubmittedAt != default)
    {
        AddInclude(sq => sq.Quiz!);
        AddOrderByDescending(sq => sq.SubmittedAt);
    }

    // GetByStudentIdForTranscriptAsync — only submitted records, no includes
    public StudentQuizSpec(int studentId, string scope)
        : base(sq => sq.StudentId == studentId && sq.SubmittedAt != default) { }

    // GetByQuizIdsAsync — all submissions for a set of quizzes, no includes
    public StudentQuizSpec(ICollection<int> quizIds, bool byQuizzes)
        : base(sq => quizIds.Contains(sq.QuizId)) { }

    // GetByStudentAndQuizIdsAsync — all submissions for a student and set of quizzes, no includes
    public StudentQuizSpec(int studentId, ICollection<int> quizIds)
        : base(sq => sq.StudentId == studentId && quizIds.Contains(sq.QuizId)) { }
}
