using System.Security.Claims;
using IntelliCampus.Shared.Dtos.Registration;
using IntelliCampus.Service_Abstraction;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IntelliCampus.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Student_UnderGrad,Student_Masters,Student_PhD")]
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

        await _registrationService.UnregisterStudentFromCourseAsync(studentId.Value, courseId);
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
