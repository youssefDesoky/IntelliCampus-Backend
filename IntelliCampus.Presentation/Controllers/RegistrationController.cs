using System.Security.Claims;
using IntelliCampus.Shared.Dtos.Registration;
using IntelliCampus.Service_Abstraction;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IntelliCampus.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Student")]
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

        try
        {
            var registration = await _registrationService.RegisterStudentInCourseAsync(studentId.Value, dto);

            if (registration is null)
                return BadRequest(new { message = "Registration failed." });

            return Ok(registration);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
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

        var result = await _registrationService.UnregisterStudentFromCourseAsync(studentId.Value, courseId);

        if (!result)
            return NotFound(new { message = "Registration not found." });

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

        // For TPT, the UserId is the same as StudentId (they share the PK)
        return userId;
    }
}
