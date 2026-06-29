using System.Security.Claims;
using IntelliCampus.Service_Abstraction;
using IntelliCampus.shared.Dtos.Quiz;
using IntelliCampus.shared.Pagination;
using IntelliCampus.Shared.Params;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IntelliCampus.Presentation.Controllers;

[ApiController]
[Authorize]
[Route("api/quizzes")]
public class QuizzesController : ControllerBase
{
    private readonly IQuizService _quizService;

    public QuizzesController(IQuizService quizService)
    {
        _quizService = quizService;
    }

    private int UserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");

    [HttpGet("{quizId}")]
    public async Task<IActionResult> GetById(int quizId)
    {
        var result = await _quizService.GetByIdAsync(quizId, UserId);
        return Ok(result);
    }

    [HttpGet("/api/courses/{courseId}/quizzes")]
    [Authorize(Roles = "Student_Bachelor,Student_Masters,Student_PhD,Student_Diploma,Instructor")]
    public async Task<ActionResult<PaginatedResult<CourseQuizzesDto>>> GetQuizzesOverview(string courseId, [FromQuery] QuizQueryParams queryParams)
    {
        var result = await _quizService.GetQuizzesOverviewAsync(UserId, courseId, queryParams);
        return Ok(result);
    }

    [HttpGet("/api/courses/{courseId}/quizzes/{quizId}")]
    public async Task<IActionResult> GetQuizById(string courseId, int quizId)
    {
        var result = await _quizService.GetByIdInCourseAsync(quizId, UserId, courseId);
        return Ok(result);
    }

    [HttpGet("/api/courses/{courseId}/quizzes/practice")]
    [Authorize(Roles = "Student_Bachelor,Student_Masters,Student_PhD,Student_Diploma")]
    public async Task<IActionResult> GetPracticeQuiz(string courseId, [FromQuery] QuizQueryParams queryParams)
    {
        var result = await _quizService.GetPracticeQuizAsync(UserId, courseId, queryParams);
        return Ok(result);
    }

    [HttpPost("/api/courses/{courseId}/quizzes/practice/submit")]
    [Authorize(Roles = "Student_Bachelor,Student_Masters,Student_PhD,Student_Diploma")]
    public async Task<IActionResult> SubmitPracticeQuiz(string courseId, [FromBody] SubmitQuizDto dto)
    {
        var result = await _quizService.SubmitPracticeQuizAsync(UserId, courseId, dto);
        return Ok(result);
    }

    [HttpPost("/api/courses/{courseId}/quizzes/{quizId}/questions")]
    [Authorize(Roles = "Instructor")]
    public async Task<IActionResult> AddQuestions(string courseId, int quizId, [FromBody] List<CreateQuestionDto> questions)
    {
        await _quizService.AddQuestionsAsync(quizId, UserId, courseId, questions);
        return Ok();
    }

    [HttpDelete("/api/courses/{courseId}/quizzes/{quizId}/questions/{questionId}")]
    [Authorize(Roles = "Instructor")]
    public async Task<IActionResult> DeleteQuestion(string courseId, int quizId, int questionId)
    {
        await _quizService.DeleteQuestionAsync(questionId, UserId, courseId);
        return Ok();
    }

    [HttpGet("/api/courses/{courseId}/quizzes/{quizId}/questions")]
    [Authorize(Roles = "Instructor")]
    public async Task<IActionResult> GetQuestions(string courseId, int quizId)
    {
        var result = await _quizService.GetQuestionsAsync(quizId, UserId, courseId);
        return Ok(result);
    }

    [HttpGet("/api/courses/{courseId}/quizzes/{quizId}/submissions")]
    [Authorize(Roles = "Instructor")]
    public async Task<IActionResult> GetSubmissions(string courseId, int quizId)
    {
        var result = await _quizService.GetSubmissionsAsync(quizId, UserId, courseId);
        return Ok(result);
    }

    [HttpPut("/api/courses/{courseId}/quizzes/{quizId}/submissions/{studentId}/grade")]
    [Authorize(Roles = "Instructor")]
    public async Task<IActionResult> GradeWritten(string courseId, int quizId, int studentId, [FromBody] GradeWrittenDto dto)
    {
        await _quizService.GradeWrittenAsync(quizId, studentId, UserId, courseId, dto);
        return Ok();
    }

    [HttpPost("/api/courses/{courseId}/quizzes")]
    [Authorize(Roles = "Instructor")]
    public async Task<IActionResult> CreateInCourse(string courseId, [FromBody] CreateQuizDto dto)
    {
        var result = await _quizService.CreateInCourseAsync(UserId, courseId, dto);
        return Ok(result);
    }

    [HttpDelete("/api/courses/{courseId}/quizzes/{quizId}")]
    [Authorize(Roles = "Instructor")]
    public async Task<IActionResult> DeleteInCourse(string courseId, int quizId)
    {
        await _quizService.DeleteInCourseAsync(quizId, UserId, courseId);
        return Ok();
    }

    [HttpPut("/api/courses/{courseId}/quizzes/{quizId}")]
    [Authorize(Roles = "Instructor")]
    public async Task<IActionResult> UpdateInCourse(string courseId, int quizId, [FromBody] UpdateQuizDto dto)
    {
        var result = await _quizService.UpdateInCourseAsync(quizId, UserId, courseId, dto);
        return Ok(result);
    }
}
