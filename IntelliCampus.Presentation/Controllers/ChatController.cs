using IntelliCampus.Service_Abstraction;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IntelliCampus.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ChatController(IChatService chatService) : ControllerBase
{
    [HttpGet("history/{userId1}/{userId2}")]
    public async Task<IActionResult> GetChatHistory(string userId1, string userId2)
        => Ok(await chatService.GetChatHistoryAsync(userId1, userId2));

    [HttpGet("group/{groupName}")]
    public async Task<IActionResult> GetGroupChatHistory(string groupName)
        => Ok(await chatService.GetGroupChatHistoryAsync(groupName));
}