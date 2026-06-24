using System.Security.Claims;
using IntelliCampus.shared.Pagination;
using IntelliCampus.Shared.Dtos.Admin;
using IntelliCampus.Shared.Params;
using IntelliCampus.Service_Abstraction;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IntelliCampus.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "SuperAdmin")]
public class AdminsController : ControllerBase
{
    private readonly IAdminService _adminService;

    public AdminsController(IAdminService adminService)
    {
        _adminService = adminService;
    }

    [HttpGet]
    public async Task<ActionResult<PaginatedResult<AdminDto>>> GetAll([FromQuery] AdminQueryParams queryParams)
    {
        var admins = await _adminService.GetAllAsync(queryParams);
        return Ok(admins);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<AdminDto>> GetById(int id)
    {
        var admin = await _adminService.GetByIdAsync(id);
        return Ok(admin);
    }

    [HttpPost]
    public async Task<ActionResult<AdminDto>> Create([FromBody] CreateAdminDto dto)
    {
        var creatorUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var admin = await _adminService.CreateAsync(dto, creatorUserId is not null ? int.Parse(creatorUserId) : null);
        return CreatedAtAction(nameof(GetById), new { id = admin.UserId }, admin);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<AdminDto>> Update(int id, [FromBody] UpdateAdminDto dto)
    {
        var admin = await _adminService.UpdateAsync(id, dto);
        return Ok(admin);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        await _adminService.DeleteAsync(id);
        return NoContent();
    }
}
