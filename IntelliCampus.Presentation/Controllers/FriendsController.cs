using System.Security.Claims;
using IntelliCampus.Domain.Entities.Enums;
using IntelliCampus.Presentation.Hubs;
using IntelliCampus.Service_Abstraction;
using IntelliCampus.Shared.Dtos.Friend;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace IntelliCampus.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class FriendsController(IFriendService friendService, IHubContext<ChatHub> hubContext, INotificationService notificationService) : ControllerBase
{
    private int UserId
        => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpPost("request")]
    public async Task<IActionResult> SendRequest([FromBody] SendFriendRequestDto dto)
    {
        var result = await friendService.SendRequestAsync(UserId, dto.RecipientId);
        await hubContext.Clients.User(result.RecipientId.ToString()).SendAsync("ReceiveFriendRequest", result);
        await notificationService.SendAsync(result.RecipientId, NotificationType.FriendRequestReceived, $"{result.SenderName} sent you a friend request.", "Friend Request", "/?openChat=addFriend");
        return Ok(result);
    }

    [HttpGet("requests/pending")]
    public async Task<IActionResult> GetPendingRequests()
        => Ok(await friendService.GetPendingRequestsAsync(UserId));

    [HttpPut("requests/{requestId}/accept")]
    public async Task<IActionResult> AcceptRequest(int requestId)
    {
        var result = await friendService.AcceptRequestAsync(requestId, UserId);
        await hubContext.Clients.User(result.SenderId.ToString()).SendAsync("FriendRequestAccepted", result);
        await notificationService.SendAsync(result.SenderId, NotificationType.FriendRequestReceived, $"{result.RecipientName} accepted your friend request.", "Friend Request Accepted");
        return Ok(result);
    }

    [HttpPut("requests/{requestId}/reject")]
    public async Task<IActionResult> RejectRequest(int requestId)
    {
        var result = await friendService.RejectRequestAsync(requestId, UserId);
        return Ok(result);
    }

    [HttpGet]
    public async Task<IActionResult> GetFriends([FromQuery] int pageIndex = 1, [FromQuery] int pageSize = 100)
        => Ok(await friendService.GetFriendsAsync(UserId, pageIndex, pageSize));

    [HttpDelete("{friendId}")]
    public async Task<IActionResult> DeleteFriend(int friendId)
    {
        await friendService.DeleteFriendAsync(UserId, friendId);
        return NoContent();
    }
}
