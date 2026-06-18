using System.Security.Claims;
using IntelliCampus.Domain.Entities.Enums;
using IntelliCampus.Service_Abstraction;
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
    [Authorize(Roles = "Admin_UnderGrad,Admin_Masters,Admin_PhD,Admin_Diploma,SuperAdmin")]
    public async Task<IActionResult> GetByStudentId(int studentId)
        => Ok(await scheduleService.GetByStudentIdAsync(studentId));

    [HttpGet("my-schedule")]
    [Authorize(Roles = "Student_UnderGrad,Student_Masters,Student_PhD,Student_Diploma")]
    public async Task<IActionResult> GetMySchedule([FromQuery(Name = "type")] ScheduleType[]? types)
    {
        if (types is null || types.Length == 0)
            return Ok(await scheduleService.GetByStudentIdAsync(UserId));

        return Ok(await scheduleService.GetByStudentIdAndTypesAsync(UserId, types));
    }

    [HttpGet("my-schedule/export")]
    [Authorize(Roles = "Student_UnderGrad,Student_Masters,Student_PhD,Student_Diploma")]
    public async Task<IActionResult> ExportMySchedule([FromQuery(Name = "type")] ScheduleType[]? types)
    {
        var pdf = await scheduleService.ExportSchedulePdfAsync(UserId, types);
        return File(pdf, "application/pdf", "WeeklySchedule.pdf");
    }
}
