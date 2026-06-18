namespace IntelliCampus.Service.Exceptions
{
    public abstract class NotFoundException(string message) : Exception(message)
    {
    }

    public sealed class QuizNotFoundException(int id) : NotFoundException($"Quiz With Id {id} Is Not Found")
    {
    }

    public sealed class CourseNotFoundException : NotFoundException
    {
        public CourseNotFoundException(int id) : base($"Course With Id {id} Is Not Found") { }
        public CourseNotFoundException(string code) : base($"Course With Code {code} Is Not Found") { }
    }

    public sealed class QuestionNotFoundException(int id) : NotFoundException($"Question With Id {id} Is Not Found")
    {
    }

    public sealed class SubmissionNotFoundException(int studentId, int quizId)
        : NotFoundException($"Submission for student {studentId} and quiz {quizId} Is Not Found")
    {
    }
}
