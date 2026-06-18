using IntelliCampus.Shared.Dtos.Specialization;
using IntelliCampus.Service_Abstraction;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IntelliCampus.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SpecializationController : ControllerBase
{
    private readonly ISpecializationService _specializationService;

    public SpecializationController(ISpecializationService specializationService)
    {
        _specializationService = specializationService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<SpecializationDto>>> GetAll()
    {
        var items = await _specializationService.GetAllAsync();
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
}
