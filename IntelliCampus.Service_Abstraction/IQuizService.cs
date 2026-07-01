using IntelliCampus.shared.Dtos.Quiz;
using IntelliCampus.shared.Pagination;
using IntelliCampus.Shared.Params;

namespace IntelliCampus.Service_Abstraction;

public interface IQuizService
{
    // Basic CRUD as requested
    Task<QuizHistoryItemDto?> GetByIdAsync(int quizId, int studentId);
    Task<QuizHistoryItemDto?> GetByIdInCourseAsync(int quizId, int studentId, string courseId);
    Task<IEnumerable<QuizDto>> GetByCourseIdAsync(int courseId);
    Task<QuizDto> CreateInCourseAsync(int instructorId, string courseId, CreateQuizDto dto);
    Task<bool> DeleteInCourseAsync(int quizId, int instructorId, string courseId);
    Task<QuizDto> UpdateInCourseAsync(int quizId, int instructorId, string courseId, UpdateQuizDto dto);
    Task AddQuestionsAsync(int quizId, int instructorId, string courseId, List<CreateQuestionDto> questions);
    Task<List<object>> GetQuestionsAsync(int quizId, int instructorId, string courseId);
    Task DeleteQuestionAsync(int questionId, int instructorId, string courseId);
    Task<List<StudentSubmissionDto>> GetSubmissionsAsync(int quizId, int instructorId, string courseId);
    Task GradeWrittenAsync(int quizId, int studentId, int instructorId, string courseId, GradeWrittenDto dto);

    Task<StudentQuizDto> SubmitAsync(int studentId, SubmitQuizDto dto);
    Task<StudentQuizDto?> GetResultAsync(int studentId, int quizId);
    Task<IEnumerable<StudentQuizDto>> GetAllResultsAsync(int quizId, int instructorId);
    Task<IEnumerable<StudentQuizDto>> GetByStudentIdAsync(int studentId);

    // Advanced JSON matching endpoints (using courseId as a string to match the frontend expectations)
    Task<QuizSubmitResponseDto?> SubmitPracticeQuizAsync(int studentId, string courseId, SubmitQuizDto dto);
    Task<PracticeQuizDto?> GetPracticeQuizAsync(int studentId, string courseId, QuizQueryParams queryParams);
    Task<CourseQuizzesDto?> GetQuizzesOverviewAsync(int studentId, string courseId);
    Task<PaginatedResult<CourseQuizzesDto>> GetQuizzesOverviewAsync(int studentId, string courseId, QuizQueryParams queryParams);
}
