using System.Security.Claims;
using IntelliCampus.Service_Abstraction;
using IntelliCampus.Shared.Dtos.Reminder;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IntelliCampus.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Student")]
public class RemindersController : ControllerBase
{
    private readonly IReminderService _reminderService;

    public RemindersController(IReminderService reminderService)
    {
        _reminderService = reminderService;
    }

    // GET api/reminders?selectedDay=2026-04-24
    [HttpGet]
    public async Task<ActionResult<RemindersGroupedDto>> Get([FromQuery] DateOnly selectedDay)
    {
        var studentId = GetCurrentStudentId();
        if (studentId is null)
            return Unauthorized();

        try
        {
            var result = await _reminderService.GetRemindersAsync(studentId.Value, selectedDay);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // POST api/reminders
    [HttpPost]
    public async Task<ActionResult<ReminderDto>> Create([FromBody] CreateReminderDto dto)
    {
        var studentId = GetCurrentStudentId();
        if (studentId is null)
            return Unauthorized();

        try
        {
            var created = await _reminderService.CreatePersonalReminderAsync(studentId.Value, dto);
            return Ok(created);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // PUT api/reminders/{id}
    [HttpPut("{id}")]
    public async Task<ActionResult<ReminderDto>> Update([FromRoute] string id, [FromBody] UpdateReminderDto dto)
    {
        var studentId = GetCurrentStudentId();
        if (studentId is null)
            return Unauthorized();

        try
        {
            var updated = await _reminderService.UpdatePersonalReminderAsync(studentId.Value, id, dto);
            if (updated is null)
                return NotFound(new { message = "Reminder not found." });

            return Ok(updated);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // DELETE api/reminders/{id}
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete([FromRoute] string id)
    {
        var studentId = GetCurrentStudentId();
        if (studentId is null)
            return Unauthorized();

        var deleted = await _reminderService.DeletePersonalReminderAsync(studentId.Value, id);
        if (!deleted)
            return NotFound(new { message = "Reminder not found." });

        return NoContent();
    }

    private int? GetCurrentStudentId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var roleClaim = User.FindFirst(ClaimTypes.Role)?.Value;

        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
            return null;

        if (roleClaim != "Student")
            return null;

        return userId;
    }
}
