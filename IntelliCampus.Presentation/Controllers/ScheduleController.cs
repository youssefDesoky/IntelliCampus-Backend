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
        return result is null ? NotFound() : Ok(result);
    }

    [HttpGet("student/{studentId}")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> GetByStudentId(int studentId)
        => Ok(await scheduleService.GetByStudentIdAsync(studentId));

    // GET api/schedule/my-schedule
    // GET api/schedule/my-schedule?type=Lecture
    // GET api/schedule/my-schedule?type=Lecture&type=Lab
    [HttpGet("my-schedule")]
    [Authorize(Roles = "Student")]
    public async Task<IActionResult> GetMySchedule([FromQuery(Name = "type")] ScheduleType[]? types)
    {
        if (types is null || types.Length == 0)
            return Ok(await scheduleService.GetByStudentIdAsync(UserId));

        return Ok(await scheduleService.GetByStudentIdAndTypesAsync(UserId, types));
    }
}
