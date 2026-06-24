using System.Security.Claims;
using IntelliCampus.Service_Abstraction;
using IntelliCampus.Shared.Dtos.Reminder;
using IntelliCampus.Shared.Params;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IntelliCampus.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Student_Bachelor,Student_Masters,Student_PhD,Student_Diploma")]
public class RemindersController : ControllerBase
{
    private readonly IReminderService _reminderService;

    public RemindersController(IReminderService reminderService)
    {
        _reminderService = reminderService;
    }

    [HttpGet]
    public async Task<ActionResult<RemindersGroupedDto>> Get([FromQuery] ReminderQueryParams queryParams)
    {
        var studentId = GetCurrentStudentId();
        if (studentId is null)
            return Unauthorized();

        var result = await _reminderService.GetRemindersAsync(studentId.Value, queryParams);
        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<ReminderDto>> Create([FromBody] CreateReminderDto dto)
    {
        var studentId = GetCurrentStudentId();
        if (studentId is null)
            return Unauthorized();

        var created = await _reminderService.CreatePersonalReminderAsync(studentId.Value, dto);
        return Ok(created);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ReminderDto>> Update([FromRoute] string id, [FromBody] UpdateReminderDto dto)
    {
        var studentId = GetCurrentStudentId();
        if (studentId is null)
            return Unauthorized();

        var updated = await _reminderService.UpdatePersonalReminderAsync(studentId.Value, id, dto);
        return Ok(updated);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete([FromRoute] string id)
    {
        var studentId = GetCurrentStudentId();
        if (studentId is null)
            return Unauthorized();

        await _reminderService.DeletePersonalReminderAsync(studentId.Value, id);
        return NoContent();
    }

    private int? GetCurrentStudentId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var roleClaims = User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();

        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
            return null;

        if (!roleClaims.Any(r => r.StartsWith("Student_")))
            return null;

        return userId;
    }
}
