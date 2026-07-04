using System.Security.Claims;
using IntelliCampus.Domain.Entities.Enums;
using IntelliCampus.Presentation.Hubs;
using IntelliCampus.Service_Abstraction;
using IntelliCampus.Shared.Dtos.ChatMessage;
using IntelliCampus.Shared.Dtos.Group;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace IntelliCampus.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class GroupsController(IGroupService groupService, IHubContext<ChatHub> hubContext, INotificationService notificationService) : ControllerBase
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

        foreach (var member in result.Members)
        {
            if (member.UserId != UserId)
                await hubContext.Clients.User(member.UserId.ToString()).SendAsync("GroupCreated");
        }

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
        if (!result) return BadRequest(new { error = "Could not add member" });

        var adminName = User.FindFirstValue(ClaimTypes.Name)!;
        var addedName = await groupService.GetUserDisplayNameAsync(userId);
        var groupName = $"group_{groupId}";

        await hubContext.Clients.Group(groupName).SendAsync("GroupMembersUpdated", new
        {
            groupName,
            content = $"{addedName ?? "A new member"} was added to the group",
            timestamp = DateTime.UtcNow
        });

        await hubContext.Clients.User(userId.ToString()).SendAsync("GroupCreated");

        var group = await groupService.GetGroupByIdAsync(groupId, UserId);
        if (group is not null)
        {
            await notificationService.SendAsync(
                userId,
                NotificationType.NewMessage,
                $"{adminName} added you to the group \"{group.Title}\"",
                "Group",
                $"/?openChat=group&userId={groupName}&userName={Uri.EscapeDataString(group.Title)}");
        }

        return Ok();
    }

    [HttpDelete("{groupId}/leave")]
    public async Task<IActionResult> LeaveGroup(int groupId)
    {
        var userName = User.FindFirstValue(ClaimTypes.Name)!;
        await groupService.RemoveMemberAsync(groupId, UserId, UserId);

        var groupName = $"group_{groupId}";
        await hubContext.Clients.Group(groupName).SendAsync("GroupMembersUpdated", new
        {
            groupName,
            content = $"{userName} left the group",
            timestamp = DateTime.UtcNow
        });

        var group = await groupService.GetGroupByIdAsync(groupId, UserId);
        if (group is not null)
        {
            var memberIds = group.Members.Select(m => m.UserId).Where(id => id != UserId).ToList();
            if (memberIds.Count > 0)
            {
                await notificationService.SendToManyAsync(
                    memberIds,
                    NotificationType.NewMessage,
                    $"{userName} left the group \"{group.Title}\"",
                    "Group",
                    $"/?openChat=group&userId={groupName}&userName={Uri.EscapeDataString(group.Title)}");
            }
        }

        return Ok();
    }

    [HttpDelete("{groupId}/members/{userId}")]
    public async Task<IActionResult> RemoveMember(int groupId, int userId)
    {
        var removedName = await groupService.GetUserDisplayNameAsync(userId);
        await groupService.RemoveMemberAsync(groupId, userId, UserId);

        var groupName = $"group_{groupId}";
        await hubContext.Clients.Group(groupName).SendAsync("GroupMembersUpdated", new
        {
            groupName,
            content = $"{removedName ?? "A member"} was removed from the group",
            timestamp = DateTime.UtcNow
        });

        var group = await groupService.GetGroupByIdAsync(groupId, UserId);
        if (group is not null)
        {
            await notificationService.SendAsync(
                userId,
                NotificationType.NewMessage,
                $"You were removed from the group \"{group.Title}\"",
                "Group",
                $"/?openChat=group&userId={groupName}&userName={Uri.EscapeDataString(group.Title)}");
        }

        return Ok();
    }
}
