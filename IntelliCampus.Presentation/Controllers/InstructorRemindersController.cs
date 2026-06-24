using System.Security.Claims;
using IntelliCampus.Service_Abstraction;
using IntelliCampus.Shared.Dtos.Reminder;
using IntelliCampus.Shared.Params;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IntelliCampus.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Instructor")]
public class InstructorRemindersController(IInstructorReminderService instructorReminderService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<RemindersGroupedDto>> Get([FromQuery] ReminderQueryParams queryParams)
    {
        var instructorId = GetCurrentInstructorId();
        if (instructorId is null)
            return Unauthorized();

        var result = await instructorReminderService.GetRemindersAsync(instructorId.Value, queryParams);
        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<ReminderDto>> Create([FromBody] CreateReminderDto dto)
    {
        var instructorId = GetCurrentInstructorId();
        if (instructorId is null)
            return Unauthorized();

        var created = await instructorReminderService.CreatePersonalReminderAsync(instructorId.Value, dto);
        return Ok(created);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ReminderDto>> Update([FromRoute] string id, [FromBody] UpdateReminderDto dto)
    {
        var instructorId = GetCurrentInstructorId();
        if (instructorId is null)
            return Unauthorized();

        var updated = await instructorReminderService.UpdatePersonalReminderAsync(instructorId.Value, id, dto);
        return Ok(updated);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete([FromRoute] string id)
    {
        var instructorId = GetCurrentInstructorId();
        if (instructorId is null)
            return Unauthorized();

        await instructorReminderService.DeletePersonalReminderAsync(instructorId.Value, id);
        return NoContent();
    }

    private int? GetCurrentInstructorId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var roleClaims = User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();

        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
            return null;

        if (!roleClaims.Contains("Instructor"))
            return null;

        return userId;
    }
}
