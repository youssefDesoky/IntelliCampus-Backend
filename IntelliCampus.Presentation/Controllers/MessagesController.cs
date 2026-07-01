using System.Security.Claims;
using IntelliCampus.Service_Abstraction;
using IntelliCampus.shared.Pagination;
using IntelliCampus.Shared.Dtos.Inbox;
using IntelliCampus.Shared.Params;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IntelliCampus.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class MessagesController(IInternalMessageService messageService) : ControllerBase
{
    private int UserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpPost]
    public async Task<IActionResult> Send([FromBody] SendMessageDto dto)
    {
        var result = await messageService.SendMessageAsync(UserId, dto.RecipientEmail, dto.Subject, dto.Body, dto.ParentMessageId);
        return Ok(result);
    }

    [HttpGet("inbox")]
    public async Task<ActionResult<PaginatedResult<InternalMessageDto>>> GetInbox([FromQuery] MessageQueryParams queryParams)
        => Ok(await messageService.GetInboxMessagesAsync(UserId, queryParams));

    [HttpGet("sent")]
    public async Task<IActionResult> GetSent([FromQuery] MessageQueryParams queryParams)
        => Ok(await messageService.GetSentMessagesAsync(UserId, queryParams));

    [HttpPut("{id}/read")]
    public async Task<IActionResult> MarkAsRead(int id)
    {
        await messageService.MarkAsReadAsync(UserId, id);
        return Ok();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        await messageService.DeleteMessageAsync(UserId, id);
        return Ok();
    }

}
