using System.Security.Claims;
using IntelliCampus.Domain.Entities.Enums;
using IntelliCampus.Service_Abstraction;
using IntelliCampus.Shared.Params;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IntelliCampus.Web.Controllers;

[ApiController]
[Route("api/instructor/schedule")]
[Authorize(Roles = "Instructor")]
public class InstructorScheduleController(IInstructorScheduleService instructorScheduleService) : ControllerBase
{
    private int UserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet("my-schedule")]
    public async Task<IActionResult> GetMySchedule([FromQuery] ScheduleQueryParams queryParams)
    {
        var result = await instructorScheduleService.GetMyScheduleAsync(UserId, queryParams);
        return Ok(result);
    }

    [HttpGet("{classId:int}")]
    public async Task<IActionResult> GetScheduleById(int classId)
    {
        var result = await instructorScheduleService.GetScheduleByIdAsync(classId, UserId);
        return Ok(result);
    }

    [HttpGet("my-schedule/export")]
    public async Task<IActionResult> ExportSchedule([FromQuery] ScheduleQueryParams queryParams)
    {
        var pdf = await instructorScheduleService.ExportSchedulePdfAsync(UserId, queryParams);
        return File(pdf, "application/pdf", "WeeklySchedule.pdf");
    }
}
