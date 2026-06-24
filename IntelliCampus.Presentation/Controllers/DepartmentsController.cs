using System.Security.Claims;
using IntelliCampus.shared.Pagination;
using IntelliCampus.Shared.Dtos.Department;
using IntelliCampus.Shared.Params;
using IntelliCampus.Service_Abstraction;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IntelliCampus.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DepartmentsController : ControllerBase
{
    private readonly IDepartmentService _departmentService;

    public DepartmentsController(IDepartmentService departmentService)
    {
        _departmentService = departmentService;
    }

    [HttpGet]
    public async Task<ActionResult<PaginatedResult<DepartmentDto>>> GetAll([FromQuery] DepartmentQueryParams queryParams)
    {
        var departments = await _departmentService.GetAllAsync(queryParams);
        return Ok(departments);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<DepartmentDto>> GetById(int id)
    {
        var department = await _departmentService.GetByIdAsync(id);

        return Ok(department);
    }

    [HttpPost]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<ActionResult<DepartmentDto>> Create([FromBody] CreateDepartmentDto dto)
    {
        var creatorUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var department = await _departmentService.CreateAsync(dto, creatorUserId is not null ? int.Parse(creatorUserId) : null);
        return CreatedAtAction(nameof(GetById), new { id = department.DepartmentId }, department);
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<ActionResult<DepartmentDto>> Update(int id, [FromBody] UpdateDepartmentDto dto)
    {
        var department = await _departmentService.UpdateAsync(id, dto);

        return Ok(department);
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> Delete(int id)
    {
        await _departmentService.DeleteAsync(id);
        return NoContent();
    }

    [HttpPut("registration-settings")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<ActionResult<IEnumerable<DepartmentDto>>> UpdateAllRegistrationSettings([FromBody] DepartmentRegistrationSettingsDto dto)
    {
        var departments = await _departmentService.UpdateAllRegistrationSettingsAsync(dto);
        return Ok(departments);
    }
}
