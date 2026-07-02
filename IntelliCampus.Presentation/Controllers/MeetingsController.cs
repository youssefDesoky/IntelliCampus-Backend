using System.Security.Claims;
using IntelliCampus.Service_Abstraction;
using IntelliCampus.Shared.Dtos.Meeting;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IntelliCampus.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class MeetingsController : ControllerBase
{
    private readonly IMeetingService _meetingService;

    public MeetingsController(IMeetingService meetingService)
    {
        _meetingService = meetingService;
    }

    [HttpGet("course/{courseId}")]
    public async Task<ActionResult<IEnumerable<MeetingDto>>> GetByCourse(int courseId)
    {
        var meetings = await _meetingService.GetByCourseIdAsync(courseId);
        return Ok(meetings);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<MeetingDto>> GetById(int id)
    {
        var meeting = await _meetingService.GetByIdAsync(id);
        if (meeting is null) return NotFound();
        return Ok(meeting);
    }

    [HttpPost]
    [Authorize(Roles = "Instructor")]
    public async Task<ActionResult<MeetingDto>> Create([FromBody] CreateMeetingDto dto)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var meeting = await _meetingService.CreateAsync(dto, userId);
        return CreatedAtAction(nameof(GetByCourse), new { courseId = meeting.CourseId }, meeting);
    }

    [HttpPost("{id}/end")]
    [Authorize(Roles = "Instructor")]
    public async Task<IActionResult> EndMeeting(int id)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        await _meetingService.EndMeetingAsync(id, userId);
        return Ok();
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Instructor")]
    public async Task<IActionResult> Delete(int id)
    {
        await _meetingService.DeleteAsync(id);
        return NoContent();
    }
}
