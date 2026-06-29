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
public class ScheduleController(IScheduleService scheduleService) : ControllerBase
{
    private int UserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet("{scheduleId}")]
    public async Task<IActionResult> GetById(int scheduleId)
    {
        var result = await scheduleService.GetByIdAsync(scheduleId);
        return Ok(result);
    }

    [HttpGet("student/{studentId}")]
    [Authorize(Roles = "Admin_Bachelor,Admin_Masters,Admin_PhD,Admin_Diploma,SuperAdmin")]
    public async Task<IActionResult> GetByStudentId(int studentId, [FromQuery] ScheduleQueryParams? queryParams = null)
        => Ok(await scheduleService.GetByStudentIdAsync(studentId, queryParams));

    [HttpGet("my-schedule")]
    [Authorize(Roles = "Student_Bachelor,Student_Masters,Student_PhD,Student_Diploma")]
    public async Task<IActionResult> GetMySchedule([FromQuery] ScheduleQueryParams queryParams)
    {
        if (queryParams.Types is null || queryParams.Types.Length == 0)
            return Ok(await scheduleService.GetByStudentIdAsync(UserId, queryParams));

        return Ok(await scheduleService.GetByStudentIdAndTypesAsync(UserId, queryParams));
    }

    [HttpGet("my-schedule/export")]
    [Authorize(Roles = "Student_Bachelor,Student_Masters,Student_PhD,Student_Diploma")]
    public async Task<IActionResult> ExportMySchedule([FromQuery] ScheduleQueryParams queryParams)
    {
        var pdf = await scheduleService.ExportSchedulePdfAsync(UserId, queryParams);
        return File(pdf, "application/pdf", "WeeklySchedule.pdf");
    }
}
