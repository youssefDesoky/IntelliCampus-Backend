using System.Security.Claims;
using IntelliCampus.Service_Abstraction;
using IntelliCampus.shared.Dtos.Quiz;
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

    // --- Standalone (non-course) endpoints under api/quizzes ---

    [HttpGet("{quizId}")]
    public async Task<IActionResult> GetById(int quizId)
    {
        var result = await _quizService.GetByIdAsync(quizId, UserId);
        return result is null ? NotFound() : Ok(result);
    }

    // --- Course-nested endpoints under api/courses/{courseId}/quizzes ---

    [HttpGet("/api/courses/{courseId}/quizzes")]
    [Authorize(Roles = "Student_UnderGrad,Student_PostGrad")]
    public async Task<IActionResult> GetQuizzesOverview(string courseId)
    {
        var result = await _quizService.GetQuizzesOverviewAsync(UserId, courseId);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpGet("/api/courses/{courseId}/quizzes/{quizId}")]
    public async Task<IActionResult> GetQuizById(string courseId, int quizId)
    {
        var result = await _quizService.GetByIdInCourseAsync(quizId, UserId, courseId);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpGet("/api/courses/{courseId}/quizzes/practice")]
    [Authorize(Roles = "Student_UnderGrad,Student_PostGrad")]
    public async Task<IActionResult> GetPracticeQuiz(string courseId ,[FromQuery] int? quizId)
    {
        var result = await _quizService.GetPracticeQuizAsync(UserId, courseId , quizId);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost("/api/courses/{courseId}/quizzes/practice/submit")]
    [Authorize(Roles = "Student_UnderGrad,Student_PostGrad")]
    public async Task<IActionResult> SubmitPracticeQuiz(string courseId, [FromBody] SubmitQuizDto dto)
    {
        try
        {
            var result = await _quizService.SubmitPracticeQuizAsync(UserId, courseId, dto);
            return result is null ? NotFound() : Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("/api/courses/{courseId}/quizzes/{quizId}/questions")]
    [Authorize(Roles = "Instructor")]
    public async Task<IActionResult> AddQuestions(string courseId, int quizId, [FromBody] List<CreateQuestionDto> questions)
    {
        try
        {
            await _quizService.AddQuestionsAsync(quizId, UserId, courseId, questions);
            return Ok();
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpDelete("/api/courses/{courseId}/quizzes/{quizId}/questions/{questionId}")]
    [Authorize(Roles = "Instructor")]
    public async Task<IActionResult> DeleteQuestion(string courseId, int quizId, int questionId)
    {
        try
        {
            await _quizService.DeleteQuestionAsync(questionId, UserId, courseId);
            return Ok();
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
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
        try
        {
            await _quizService.GradeWrittenAsync(quizId, studentId, UserId, courseId, dto);
            return Ok();
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpPost("/api/courses/{courseId}/quizzes")]
    [Authorize(Roles = "Instructor")]
    public async Task<IActionResult> CreateInCourse(string courseId, [FromBody] CreateQuizDto dto)
    {
        try
        {
            var result = await _quizService.CreateInCourseAsync(UserId, courseId, dto);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpDelete("/api/courses/{courseId}/quizzes/{quizId}")]
    [Authorize(Roles = "Instructor")]
    public async Task<IActionResult> DeleteInCourse(string courseId, int quizId)
    {
        try
        {
            var success = await _quizService.DeleteInCourseAsync(quizId, UserId, courseId);
            return success ? Ok() : NotFound();
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }
}
