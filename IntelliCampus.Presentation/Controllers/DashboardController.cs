using System.Security.Claims;
using IntelliCampus.Service_Abstraction;
using IntelliCampus.Shared.Dtos.Dashboard;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IntelliCampus.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DashboardController : ControllerBase
{
    private readonly IDashboardService _dashboardService;

    public DashboardController(IDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    [HttpGet("stats")]
    public async Task<ActionResult<DashboardStatsDto>> GetStats()
    {
        var stats = await _dashboardService.GetStatsAsync();
        return Ok(stats);
    }

    [HttpGet("student")]
    public async Task<ActionResult<StudentDashboardDto>> GetStudentDashboard()
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var dashboard = await _dashboardService.GetStudentDashboardAsync(userId);
        return Ok(dashboard);
    }

    [HttpGet("admin")]
    [Authorize(Roles = "SuperAdmin,Admin_Bachelor,Admin_Masters,Admin_PhD,Admin_Diploma,Admin_AcademicStaff")]
    public async Task<ActionResult<AdminDashboardDto>> GetAdminDashboard()
    {
        var dashboard = await _dashboardService.GetAdminDashboardAsync();
        return Ok(dashboard);
    }

    [HttpPost("admin/news")]
    [Authorize(Roles = "SuperAdmin,Admin_Bachelor,Admin_Masters,Admin_PhD,Admin_Diploma,Admin_AcademicStaff")]
    public async Task<ActionResult<LatestNewsItemDto>> PublishNews([FromBody] PublishNewsDto dto)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var news = await _dashboardService.PublishNewsAsync(userId, dto.Title);
        return Ok(news);
    }
}
