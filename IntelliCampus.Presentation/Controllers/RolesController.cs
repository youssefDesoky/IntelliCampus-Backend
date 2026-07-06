using IntelliCampus.Shared.Dtos.Role;
using IntelliCampus.Service_Abstraction;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IntelliCampus.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RolesController : ControllerBase
{
    private readonly IRoleService _roleService;

    public RolesController(IRoleService roleService)
    {
        _roleService = roleService;
    }

    [HttpGet]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<ActionResult<IEnumerable<RoleDto>>> GetAllRoles()
    {
        var roles = await _roleService.GetAllRolesAsync();
        return Ok(roles);
    }

    [HttpGet("assignable")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<ActionResult<IEnumerable<RoleDto>>> GetAssignableRoles()
    {
        var roles = await _roleService.GetAssignableRolesAsync();
        return Ok(roles);
    }

    [HttpGet("user/{userId}")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<ActionResult<IEnumerable<UserRoleDto>>> GetUserRoles(int userId)
    {
        var userRoles = await _roleService.GetUserRolesAsync(userId);
        return Ok(userRoles);
    }

    [HttpPost("assign")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<ActionResult<UserRoleDto>> AssignRole([FromBody] AssignRoleDto dto)
    {
        var userRole = await _roleService.AssignRoleAsync(dto);
        return Ok(userRole);
    }

    [HttpPatch("user/{userId}/role/{roleId}")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<ActionResult<UserRoleDto>> UpdateUserRole(int userId, int roleId, [FromBody] UpdateUserRoleDto dto)
    {
        var userRole = await _roleService.UpdateUserRoleAsync(userId, roleId, dto);
        return Ok(userRole);
    }

    [HttpDelete("user/{userId}/role/{roleId}")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> RemoveRole(int userId, int roleId)
    {
        await _roleService.RemoveRoleAsync(userId, roleId);
        return NoContent();
    }
}
