using IntelliCampus.Service_Abstraction;
using IntelliCampus.Shared.Dtos.Quiz;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Threading.Tasks;

namespace IntelliCampus.Web.Controllers
{
    [ApiController]
    [Route("api/courses/{courseId}/quizzes")]
    public class QuizzesController : ControllerBase
    {
        private readonly IQuizService _quizService;

        public QuizzesController(IQuizService quizService)
        {
            _quizService = quizService;
        }

        [HttpGet]
        [Authorize]
        public async Task<ActionResult<CourseQuizzesDto>> GetCourseQuizzes(int courseId)
        {
            var userId = GetCurrentUserId();
            if (userId is null)
                return Unauthorized();

            var quizzes = await _quizService.GetCourseQuizzesAsync(courseId, userId.Value);
            if (quizzes == null)
            {
                return NotFound();
            }
            return Ok(quizzes);
        }

        [HttpGet("{quizId}/start")]
        [Authorize]
        public async Task<ActionResult<QuizStartDto>> StartQuiz(int courseId, int quizId)
        {
            var userId = GetCurrentUserId();
            if (userId is null)
                return Unauthorized();

            var result = await _quizService.GetQuizStartAsync(quizId, userId.Value);
            if (result == null)
            {
                return NotFound();
            }
            return Ok(result);
        }

        [HttpPost("{quizId}/grade")]
        [Authorize]
        public async Task<ActionResult<QuizSubmissionResultDto>> GradeQuiz(int courseId, int quizId, GradeQuizSubmissionDto dto)
        {
            var userId = GetCurrentUserId();
            if (userId is null)
                return Unauthorized();

            dto.QuizId = quizId;
            var result = await _quizService.GradeQuizSubmissionAsync(userId.Value, dto);
            if (result == null)
                return NotFound();
            return Ok(result);
        }

        private int? GetCurrentUserId()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
            {
                return null;
            }
            return int.Parse(userId);
        }
    }
}
