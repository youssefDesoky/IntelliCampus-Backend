using System.Security.Claims;
using IntelliCampus.Domain.Entities.Enums;
using IntelliCampus.Service_Abstraction;
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
    public async Task<IActionResult> GetMySchedule([FromQuery(Name = "type")] ScheduleType[]? types)
    {
        var result = await instructorScheduleService.GetMyScheduleAsync(UserId, types);
        return Ok(result);
    }

    [HttpGet("{classId:int}")]
    public async Task<IActionResult> GetScheduleById(int classId)
    {
        var result = await instructorScheduleService.GetScheduleByIdAsync(classId);
        return Ok(result);
    }

    [HttpGet("my-schedule/export")]
    public async Task<IActionResult> ExportSchedule([FromQuery(Name = "type")] ScheduleType[]? types)
    {
        var pdf = await instructorScheduleService.ExportSchedulePdfAsync(UserId, types);
        return File(pdf, "application/pdf", "WeeklySchedule.pdf");
    }
}
