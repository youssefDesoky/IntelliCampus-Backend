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
