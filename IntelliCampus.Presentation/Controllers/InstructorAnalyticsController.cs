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
    private readonly IInstructorAnalyticsService _analyticsService;

    public InstructorAnalyticsController(IInstructorAnalyticsService analyticsService)
    {
        _analyticsService = analyticsService;
    }

    [HttpGet("instructor/course/{courseId}")]
    public async Task<ActionResult<CourseAnalyticsDto>> GetCourseAnalytics(int courseId)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
            return Unauthorized();

        var result = await _analyticsService.GetCourseAnalyticsAsync(courseId, userId.Value);
        return Ok(result);
    }

    private int? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
            return null;
        return userId;
    }
}
