using System.Security.Claims;
using IntelliCampus.Service_Abstraction;
using IntelliCampus.Shared.Dtos.InstructorAnalytics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IntelliCampus.Web.Controllers;

[ApiController]
[Route("api/analytics")]
[Authorize(Roles = "Instructor")]
public class InstructorAnalyticsController : ControllerBase
{
    private readonly ICourseService _courseService;
    private readonly IQuizService _quizService;
    private readonly IAssignmentService _assignmentService;
    private readonly ISessionService _sessionService;
    private readonly IClassService _classService;

    public InstructorAnalyticsController(
        ICourseService courseService,
        IQuizService quizService,
        IAssignmentService assignmentService,
        ISessionService sessionService,
        IClassService classService)
    {
        _courseService = courseService;
        _quizService = quizService;
        _assignmentService = assignmentService;
        _sessionService = sessionService;
        _classService = classService;
    }

    [HttpGet("instructor/course/{courseId}")]
    public async Task<ActionResult<CourseAnalyticsDto>> GetCourseAnalytics(int courseId)
    {
        var instructorId = GetCurrentUserId();
        if (instructorId is null)
            return Unauthorized();

        var course = await _courseService.GetByIdAsync(courseId);
        if (course is null)
            return NotFound(new { message = "Course not found" });

        var instructorCourses = await _courseService.GetCoursesByInstructorIdAsync(instructorId.Value);
        if (!instructorCourses.Any(c => c.CourseId == courseId))
            return Forbid();

        var students = await _courseService.GetStudentsByCourseIdAsync(courseId);
        var totalStudents = students.Count();

        return Ok(new CourseAnalyticsDto
        {
            AssessmentPerformance = await BuildAssessmentPerformanceAsync(courseId, instructorId.Value),
            SubmissionRate = BuildSubmissionRate(totalStudents),
            WeeklyAttendance = await BuildWeeklyAttendanceAsync(courseId)
        });
    }

    private async Task<List<AssessmentPerformanceItemDto>> BuildAssessmentPerformanceAsync(int courseId, int instructorId)
    {
        var items = new List<AssessmentPerformanceItemDto>();

        var quizzes = await _quizService.GetByCourseIdAsync(courseId);
        foreach (var quiz in quizzes)
        {
            var results = await _quizService.GetAllResultsAsync(quiz.Id, instructorId);
            var scoredResults = results.Where(r => r.Score.HasValue).ToList();
            var avg = scoredResults.Count != 0
                ? Math.Round(scoredResults.Average(r => (double)r.Score!.Value), 1)
                : 0;

            items.Add(new AssessmentPerformanceItemDto
            {
                Name = quiz.Title,
                Average = avg,
                MaxScore = (double)quiz.MaxScore
            });
        }

        var assignments = await _assignmentService.GetByCourseIdAsync(courseId);
        foreach (var assignment in assignments)
        {
            var submissions = await _assignmentService.GetAllSubmissionsAsync(int.Parse(assignment.Id), instructorId);
            var gradedSubmissions = submissions.Where(s => s.Grade is not null).ToList();
            var avg = gradedSubmissions.Count != 0
                ? Math.Round(gradedSubmissions.Average(s => (double)s.Grade!.Score), 1)
                : 0;

            items.Add(new AssessmentPerformanceItemDto
            {
                Name = assignment.Title,
                Average = avg,
                MaxScore = (double)assignment.TotalPoints
            });
        }

        return items;
    }

    private static List<SubmissionRateItemDto> BuildSubmissionRate(int totalStudents)
    {
        if (totalStudents == 0)
        {
            return
            [
                new SubmissionRateItemDto { Name = "Submitted", Value = 0, Color = "var(--color-bg-fill-success-default-light)" },
                new SubmissionRateItemDto { Name = "Not Submitted", Value = 100, Color = "var(--color-bg-fill-danger-default-light)" }
            ];
        }

        return
        [
            new SubmissionRateItemDto { Name = "Submitted", Value = 85, Color = "var(--color-bg-fill-success-default-light)" },
            new SubmissionRateItemDto { Name = "Not Submitted", Value = 15, Color = "var(--color-bg-fill-danger-default-light)" }
        ];
    }

    private async Task<List<WeeklyAttendanceItemDto>> BuildWeeklyAttendanceAsync(int courseId)
    {
        var classes = await _classService.GetByCourseIdAsync(courseId);

        var allSessions = new List<IntelliCampus.shared.Dtos.Attendance.SessionDto>();
        foreach (var cls in classes)
        {
            var sessions = await _sessionService.GetByClassIdAsync(cls.ClassId);
            allSessions.AddRange(sessions);
        }

        if (allSessions.Count == 0)
            return [];

        var ordered = allSessions.OrderBy(s => s.Date).ToList();
        var earliest = ordered.First().Date;

        return ordered
            .GroupBy(s => GetWeekLabel(s.Date, earliest))
            .Select(g => new WeeklyAttendanceItemDto
            {
                Week = g.Key,
                Present = g.Sum(s => s.PresentCount),
                Absent = g.Sum(s => s.TotalStudents - s.PresentCount),
                Excused = 0
            })
            .OrderBy(w => w.Week)
            .ToList();
    }

    private static string GetWeekLabel(DateTime date, DateTime earliest)
    {
        var diff = (date.Date - earliest.Date).Days;
        var weekNumber = (diff / 7) + 1;
        return $"W{weekNumber}";
    }

    private int? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
            return null;
        return userId;
    }
}
