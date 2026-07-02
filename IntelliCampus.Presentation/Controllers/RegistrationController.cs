using System.Security.Claims;
using IntelliCampus.Shared.Dtos.Registration;
using IntelliCampus.Service_Abstraction;
using IntelliCampus.Service_Abstraction.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IntelliCampus.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Student_Bachelor,Student_Masters,Student_PhD,Student_Diploma")]
public class RegistrationController : ControllerBase
{
    private readonly IRegistrationService _registrationService;

    public RegistrationController(IRegistrationService registrationService)
    {
        _registrationService = registrationService;
    }

    [HttpPost]
    public async Task<ActionResult<StudentRegistrationDto>> Register(CourseRegistrationDto dto)
    {
        var studentId = GetCurrentStudentId();
        if (studentId is null)
            return Unauthorized();

        var registration = await _registrationService.RegisterStudentInCourseAsync(studentId.Value, dto);
        return Ok(registration);
    }

    [HttpGet("my-courses")]
    public async Task<ActionResult<IEnumerable<StudentRegistrationDto>>> GetMyRegistrations()
    {
        var studentId = GetCurrentStudentId();
        if (studentId is null)
            return Unauthorized();

        var registrations = await _registrationService.GetStudentRegistrationsAsync(studentId.Value);
        return Ok(registrations);
    }

    [HttpDelete("{courseId}")]
    public async Task<IActionResult> Unregister(int courseId)
    {
        var studentId = GetCurrentStudentId();
        if (studentId is null)
            return Unauthorized();

        try
        {
            var result = await _registrationService.UnregisterStudentFromCourseAsync(studentId.Value, courseId);
            if (result)
                return Ok(new { success = true, message = $"Successfully unregistered from course" });
            return StatusCode(500, new { success = false, message = "Unregistration failed." });
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { success = false, message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    [HttpGet("settings")]
    public async Task<ActionResult<RegistrationSettingsDto>> GetSettings()
    {
        var studentId = GetCurrentStudentId();
        if (studentId is null)
            return Unauthorized();

        var settings = await _registrationService.GetRegistrationSettingsAsync(studentId.Value);
        return Ok(settings);
    }

    [HttpPatch("{courseId}/section")]
    public async Task<IActionResult> ChangeSection(int courseId, [FromBody] ChangeSectionDto dto)
    {
        var studentId = GetCurrentStudentId();
        if (studentId is null)
            return Unauthorized();

        await _registrationService.ChangeStudentCourseSectionAsync(studentId.Value, courseId, dto.ClassId);
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
