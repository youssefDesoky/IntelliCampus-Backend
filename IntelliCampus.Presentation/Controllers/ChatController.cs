using IntelliCampus.Service_Abstraction;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace IntelliCampus.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ChatController(IChatService chatService, IFileStorageService fileStorage) : ControllerBase
{
    [HttpGet("history/{userId1}/{userId2}")]
    public async Task<IActionResult> GetChatHistory(string userId1, string userId2)
        => Ok(await chatService.GetChatHistoryAsync(userId1, userId2));

    [HttpGet("group/{groupName}")]
    public async Task<IActionResult> GetGroupChatHistory(string groupName)
        => Ok(await chatService.GetGroupChatHistoryAsync(groupName));

    [HttpPost("upload")]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<IActionResult> UploadFile(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { error = "No file provided" });

        var path = await fileStorage.SaveAsync(file, "chat");
        return Ok(new { url = "/" + path });
    }
}