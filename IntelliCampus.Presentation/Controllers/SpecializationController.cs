using IntelliCampus.Shared.Dtos.Specialization;
using IntelliCampus.Service_Abstraction;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IntelliCampus.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Student_Bachelor,Admin_Bachelor,Admin_Masters,Admin_PhD,Admin_Diploma,SuperAdmin,Instructor")]
public class SpecializationController : ControllerBase
{
    private readonly ISpecializationService _specializationService;

    public SpecializationController(ISpecializationService specializationService)
    {
        _specializationService = specializationService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<SpecializationDto>>> GetAll([FromQuery] string? search = null)
    {
        var items = await _specializationService.GetAllAsync(search);
        return Ok(items);
    }

    [HttpGet("department/{departmentId}")]
    public async Task<ActionResult<IEnumerable<SpecializationDto>>> GetByDepartment(int departmentId)
    {
        var items = await _specializationService.GetByDepartmentAsync(departmentId);
        return Ok(items);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<SpecializationDto>> GetById(int id)
    {
        var item = await _specializationService.GetByIdAsync(id);
        return Ok(item);
    }

    [HttpPost]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<ActionResult<SpecializationDto>> Create([FromBody] CreateSpecializationDto dto)
    {
        var item = await _specializationService.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = item.SpecializationId }, item);
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<ActionResult<SpecializationDto>> Update(int id, [FromBody] UpdateSpecializationDto dto)
    {
        var item = await _specializationService.UpdateAsync(id, dto);
        return Ok(item);
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> Delete(int id)
    {
        await _specializationService.DeleteAsync(id);
        return NoContent();
    }

    [HttpGet("{specializationId}/prerequisites")]
    public async Task<ActionResult<IEnumerable<SpecializationPrerequisiteDto>>> GetPrerequisites(int specializationId)
    {
        var items = await _specializationService.GetPrerequisitesAsync(specializationId);
        return Ok(items);
    }

    [HttpPut("{specializationId}/prerequisites")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> SetPrerequisites(int specializationId, [FromBody] SetSpecializationPrerequisitesDto dto)
    {
        await _specializationService.SetPrerequisitesAsync(specializationId, dto);
        return NoContent();
    }
}
