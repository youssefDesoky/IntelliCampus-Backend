using System.Security.Claims;
using IntelliCampus.Service_Abstraction;
using IntelliCampus.Shared.Dtos.Friend;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IntelliCampus.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class FriendsController(IFriendService friendService) : ControllerBase
{
    private int UserId
        => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpPost("request")]
    public async Task<IActionResult> SendRequest([FromBody] SendFriendRequestDto dto)
    {
        var result = await friendService.SendRequestAsync(UserId, dto.RecipientId);
        return Ok(result);
    }

    [HttpGet("requests/pending")]
    public async Task<IActionResult> GetPendingRequests()
        => Ok(await friendService.GetPendingRequestsAsync(UserId));

    [HttpPut("requests/{requestId}/accept")]
    public async Task<IActionResult> AcceptRequest(int requestId)
    {
        var result = await friendService.AcceptRequestAsync(requestId, UserId);
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
}
