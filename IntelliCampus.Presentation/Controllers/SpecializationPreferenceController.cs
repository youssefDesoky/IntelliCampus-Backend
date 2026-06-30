using System.Security.Claims;
using IntelliCampus.Shared.Dtos.SpecializationPreference;
using IntelliCampus.Service_Abstraction;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IntelliCampus.Web.Controllers;

[ApiController]
[Route("api/specialization-preference")]
[Authorize]
public class SpecializationPreferenceController : ControllerBase
{
    private readonly ISpecializationPreferenceService _specializationPreferenceService;

    public SpecializationPreferenceController(ISpecializationPreferenceService specializationPreferenceService)
    {
        _specializationPreferenceService = specializationPreferenceService;
    }

    private int GetStudentId()
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.Parse(claim ?? "0");
    }

    [HttpGet("eligibility")]
    [Authorize(Roles = "Student_Bachelor")]
    public async Task<ActionResult<SpecializationPreferenceEligibilityDto>> GetEligibility()
    {
        var result = await _specializationPreferenceService.GetEligibilityAsync(GetStudentId());
        return Ok(result);
    }

    [HttpGet]
    [Authorize(Roles = "Student_Bachelor")]
    public async Task<ActionResult<SpecializationPreferenceDto>> GetPreferences()
    {
        var result = await _specializationPreferenceService.GetPreferencesAsync(GetStudentId());
        return Ok(result);
    }

    [HttpPut]
    [Authorize(Roles = "Student_Bachelor")]
    public async Task<IActionResult> SavePreferences([FromBody] SaveSpecializationPreferenceDto dto)
    {
        await _specializationPreferenceService.SavePreferencesAsync(GetStudentId(), dto);
        return NoContent();
    }
}
