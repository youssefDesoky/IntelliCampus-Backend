using IntelliCampus.Shared.Dtos.Quiz;
using System.Threading.Tasks;

namespace IntelliCampus.Service_Abstraction
{
    public interface IQuizService
    {
        Task<CourseQuizzesDto> GetCourseQuizzesAsync(int courseId, int studentId);
        Task<QuizStartDto> GetQuizStartAsync(int quizId, int studentId);
        Task<QuizSubmissionResultDto?> GradeQuizSubmissionAsync(int instructorId, GradeQuizSubmissionDto dto);
    }
}
