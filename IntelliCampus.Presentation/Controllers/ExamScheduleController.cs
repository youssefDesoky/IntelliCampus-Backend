using System.Security.Claims;
using IntelliCampus.Domain.Entities.Enums;
using IntelliCampus.Service_Abstraction;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IntelliCampus.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ExamScheduleController(IExamScheduleService examScheduleService) : ControllerBase
{
    private int UserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet("{examScheduleId}")]
    public async Task<IActionResult> GetById(int examScheduleId)
    {
        var result = await examScheduleService.GetByIdAsync(examScheduleId);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpGet("my-exams")]
    [Authorize(Roles = "Student")]
    public async Task<IActionResult> GetMyExams()
        => Ok(await examScheduleService.GetByStudentIdAsync(UserId));

    [HttpGet("my-exams/midterms")]
    [Authorize(Roles = "Student")]
    public async Task<IActionResult> GetMidterms()
        => Ok(await examScheduleService.GetByTypeAsync(UserId, ExamType.Midterm));

    [HttpGet("my-exams/finals")]
    [Authorize(Roles = "Student")]
    public async Task<IActionResult> GetFinals()
        => Ok(await examScheduleService.GetByTypeAsync(UserId, ExamType.Final));

    [HttpGet("my-exams/upcoming")]
    [Authorize(Roles = "Student")]
    public async Task<IActionResult> GetUpcoming()
        => Ok(await examScheduleService.GetByStatusAsync(UserId, ExamStatus.Upcoming));

    [HttpGet("my-exams/export")]
    [Authorize(Roles = "Student")]
    public async Task<IActionResult> ExportMyExams([FromQuery] ExamType? type, [FromQuery] ExamStatus? status)
    {
        var pdf = await examScheduleService.ExportExamSchedulePdfAsync(UserId, type, status);
        return File(pdf, "application/pdf", "ExamSchedule.pdf");
    }
}
