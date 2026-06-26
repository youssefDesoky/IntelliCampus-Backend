using System.Security.Claims;
using IntelliCampus.Service_Abstraction;
using IntelliCampus.Shared.Dtos.InstructorAnalytics;
using IntelliCampus.Shared.Params;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IntelliCampus.Web.Controllers;

[ApiController]
[Route("api/analytics")]
[Authorize(Roles = "Instructor")]
public class InstructorAnalyticsController : ControllerBase
{
    private readonly IInstructorAnalyticsService _instructorAnalyticsService;

    public InstructorAnalyticsController(IInstructorAnalyticsService instructorAnalyticsService)
    {
        _instructorAnalyticsService = instructorAnalyticsService;
    }

    [HttpGet("instructor/course/{courseId}")]
    public async Task<ActionResult<CourseAnalyticsDto>> GetCourseAnalytics(int courseId)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
            return Unauthorized();

        var course = await _courseService.GetByIdAsync(courseId);
        if (course is null)
            return NotFound(new { message = "Course not found" });

        var instructorRepo = _unitOfWork.GetRepository<Instructor, int>();
        var instructors = await instructorRepo.GetAllAsync();
        var instructor = instructors.FirstOrDefault(i => i.UserId == userId.Value);
        if (instructor is null)
            return Forbid();

        var instructorId = instructor.InstructorId;

        var instructorCourses = await _courseService.GetCoursesByInstructorIdAsync(new CourseQueryParams { InstructorId = instructorId });
        if (!instructorCourses.Any(c => c.CourseId == courseId))
            return Forbid();

        var students = await _courseService.GetStudentsByCourseIdAsync(courseId);
        var totalStudents = students.Count();

        return Ok(new CourseAnalyticsDto
        {
            AssessmentPerformance = await BuildAssessmentPerformanceAsync(courseId, instructorId),
            SubmissionRate = await BuildSubmissionRateAsync(courseId, instructorId, totalStudents),
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

    private async Task<List<SubmissionRateItemDto>> BuildSubmissionRateAsync(int courseId, int instructorId, int totalStudents)
    {
        if (totalStudents == 0)
            return
            [
                new SubmissionRateItemDto { Name = "Submitted", Value = 0, Color = "var(--color-bg-fill-success-default-light)" },
                new SubmissionRateItemDto { Name = "Not Submitted", Value = 100, Color = "var(--color-bg-fill-danger-default-light)" }
            ];

        var submittedStudentIds = new HashSet<int>();

        foreach (var quiz in await _quizService.GetByCourseIdAsync(courseId))
        {
            try
            {
                foreach (var r in await _quizService.GetAllResultsAsync(quiz.Id, instructorId))
                    submittedStudentIds.Add(r.StudentId);
            }
            catch { }
        }

        foreach (var assignment in await _assignmentService.GetByCourseIdAsync(courseId))
        {
            try
            {
                foreach (var s in await _assignmentService.GetAllSubmissionsAsync(int.Parse(assignment.Id), instructorId))
                    submittedStudentIds.Add(s.StudentId);
            }
            catch { }
        }

        var submittedPct = (int)Math.Round((double)submittedStudentIds.Count / totalStudents * 100);

        return
        [
            new SubmissionRateItemDto { Name = "Submitted", Value = submittedPct, Color = "var(--color-bg-fill-success-default-light)" },
            new SubmissionRateItemDto { Name = "Not Submitted", Value = 100 - submittedPct, Color = "var(--color-bg-fill-danger-default-light)" }
        ];
        var course = await _instructorAnalyticsService.GetCourseAnalyticsAsync(courseId, userId.Value);
        return Ok(course);
    }

    [HttpGet("instructor/course/{courseId}/export")]
    public async Task<IActionResult> ExportCourseAnalytics(int courseId)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
            return Unauthorized();

        var pdf = await _instructorAnalyticsService.ExportCourseAnalyticsPdfAsync(courseId, userId.Value);
        return File(pdf, "application/pdf", $"CourseAnalytics_{courseId}.pdf");
    }

    private int? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
            return null;
        return userId;
    }
}
