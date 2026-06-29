using System.Security.Claims;
using IntelliCampus.Domain.Entities.Enums;
using IntelliCampus.Service_Abstraction;
using IntelliCampus.Shared.Params;
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
        return Ok(result);
    }

    [HttpGet("my-exams")]
    [Authorize(Roles = "Student_Bachelor,Student_Masters,Student_PhD,Student_Diploma")]
    public async Task<IActionResult> GetMyExams([FromQuery] ExamScheduleQueryParams? queryParams = null)
        => Ok(await examScheduleService.GetByStudentIdAsync(UserId, queryParams));

    [HttpGet("my-exams/midterms")]
    [Authorize(Roles = "Student_Bachelor,Student_Masters,Student_PhD,Student_Diploma")]
    public async Task<IActionResult> GetMidterms([FromQuery] ExamScheduleQueryParams? queryParams = null)
        => Ok(await examScheduleService.GetByTypeAsync(UserId, ExamType.Midterm, queryParams));

    [HttpGet("my-exams/finals")]
    [Authorize(Roles = "Student_Bachelor,Student_Masters,Student_PhD,Student_Diploma")]
    public async Task<IActionResult> GetFinals([FromQuery] ExamScheduleQueryParams? queryParams = null)
        => Ok(await examScheduleService.GetByTypeAsync(UserId, ExamType.Final, queryParams));

    [HttpGet("my-exams/upcoming")]
    [Authorize(Roles = "Student_Bachelor,Student_Masters,Student_PhD,Student_Diploma")]
    public async Task<IActionResult> GetUpcoming([FromQuery] ExamScheduleQueryParams? queryParams = null)
        => Ok(await examScheduleService.GetByStatusAsync(UserId, ExamStatus.Upcoming, queryParams));

    [HttpGet("my-exams/export")]
    [Authorize(Roles = "Student_Bachelor,Student_Masters,Student_PhD,Student_Diploma")]
    public async Task<IActionResult> ExportMyExams([FromQuery] ExamScheduleQueryParams queryParams)
    {
        var pdf = await examScheduleService.ExportExamSchedulePdfAsync(UserId, queryParams);
        return File(pdf, "application/pdf", "ExamSchedule.pdf");
    }
}
