using System.Security.Claims;
using IntelliCampus.Service_Abstraction;
using IntelliCampus.Shared.Dtos.Group;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IntelliCampus.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class GroupsController(IGroupService groupService) : ControllerBase
{
    private int UserId
        => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpPost]
    public async Task<IActionResult> CreateGroup([FromBody] CreateGroupDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Title))
            return BadRequest(new { error = "Group title is required" });

        if (dto.MemberIds.Count == 0)
            return BadRequest(new { error = "At least one member must be selected" });

        var result = await groupService.CreateGroupAsync(UserId, dto.Title, dto.Description, dto.MemberIds, dto.ProfileImage);
        return Ok(result);
    }

    [HttpGet]
    public async Task<IActionResult> GetMyGroups()
        => Ok(await groupService.GetUserGroupsAsync(UserId));

    [HttpGet("{groupId}")]
    public async Task<IActionResult> GetGroupById(int groupId)
    {
        return Ok(await groupService.GetGroupByIdAsync(groupId, UserId));
    }

    [HttpPost("{groupId}/members/{userId}")]
    public async Task<IActionResult> AddMember(int groupId, int userId)
    {
        var result = await groupService.AddMemberAsync(groupId, userId, UserId);
        return result ? Ok() : BadRequest(new { error = "Could not add member" });
    }

    [HttpDelete("{groupId}/members/{userId}")]
    public async Task<IActionResult> RemoveMember(int groupId, int userId)
    {
        await groupService.RemoveMemberAsync(groupId, userId, UserId);
        return Ok();
    }
}
