using System.Security.Claims;
using IntelliCampus.Shared.Dtos.DepartmentPreference;
using IntelliCampus.Service_Abstraction;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IntelliCampus.Web.Controllers;

[ApiController]
[Route("api/department-preference")]
[Authorize]
public class DepartmentPreferenceController : ControllerBase
{
    private readonly IDepartmentPreferenceService _departmentPreferenceService;

    public DepartmentPreferenceController(IDepartmentPreferenceService departmentPreferenceService)
    {
        _departmentPreferenceService = departmentPreferenceService;
    }

    private int GetStudentId()
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.Parse(claim ?? "0");
    }

    [HttpGet("eligibility")]
    [Authorize(Roles = "Student_Bachelor")]
    public async Task<ActionResult<DepartmentPreferenceEligibilityDto>> GetEligibility()
    {
        var result = await _departmentPreferenceService.GetEligibilityAsync(GetStudentId());
        return Ok(result);
    }

    [HttpGet]
    [Authorize(Roles = "Student_Bachelor")]
    public async Task<ActionResult<DepartmentPreferenceDto>> GetPreferences()
    {
        var result = await _departmentPreferenceService.GetPreferencesAsync(GetStudentId());
        return Ok(result);
    }

    [HttpPut]
    [Authorize(Roles = "Student_Bachelor")]
    public async Task<IActionResult> SavePreferences([FromBody] SaveDepartmentPreferenceDto dto)
    {
        await _departmentPreferenceService.SavePreferencesAsync(GetStudentId(), dto);
        return NoContent();
    }
}
