using System.Security.Claims;
using System.Text;
using System.Text.Json;
using IntelliCampus.Service_Abstraction;
using IntelliCampus.shared.Pagination;
using IntelliCampus.Shared.Dtos.Notification;
using IntelliCampus.Shared.Params;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IntelliCampus.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class NotificationsController(INotificationService notificationService, INotificationStreamService notificationStreamService)
    : ControllerBase
{
    private int UserId
        => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet]
    public async Task<ActionResult<PaginatedResult<NotificationDto>>> GetAll([FromQuery] NotificationQueryParams queryParams)
        => Ok(await notificationService.GetByUserIdAsync(UserId, queryParams));

    [HttpGet("unread")]
    public async Task<IActionResult> GetUnread([FromQuery] NotificationQueryParams queryParams)
        => Ok(await notificationService.GetUnreadAsync(UserId, queryParams));

    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary([FromQuery] NotificationQueryParams queryParams)
        => Ok(await notificationService.GetSummaryAsync(UserId, queryParams));

    [HttpGet("unread/count")]
    public async Task<IActionResult> GetUnreadCount()
        => Ok(new { count = await notificationService.GetUnreadCountAsync(UserId) });

    [HttpPut("{notificationId}/read")]
    public async Task<IActionResult> MarkAsRead(int notificationId)
    {
        await notificationService.MarkAsReadAsync(notificationId, UserId);
        return Ok();
    }

    [HttpPut("read-all")]
    public async Task<IActionResult> MarkAllAsRead()
    {
        await notificationService.MarkAllAsReadAsync(UserId);
        return Ok();
    }

    [HttpDelete("{notificationId}")]
    public async Task<IActionResult> Delete(int notificationId)
    {
        await notificationService.DeleteAsync(notificationId, UserId);
        return Ok();
    }

    [HttpPost("send")]
    [Authorize(Roles = "Admin_Bachelor,Admin_Masters,Admin_PhD,Admin_Diploma,SuperAdmin,Instructor")]
    public async Task<IActionResult> SendBulk(SendBulkNotificationDto dto)
    {
        if (dto.UserIds is null || dto.UserIds.Count == 0)
            return BadRequest("At least one user must be specified.");

        await notificationService.SendToManyAsync(dto.UserIds, dto.Type, dto.Message, dto.Title, dto.ClickUrl, dto.ImageUrl);

        return Ok(new { sent = dto.UserIds.Count });
    }

    [HttpGet("stream")]
    public async Task StreamNotifications(CancellationToken cancellationToken)
    {
        Response.ContentType = "text/event-stream";
        Response.Headers["Cache-Control"] = "no-cache";
        Response.Headers["Connection"] = "keep-alive";

        var subscription = notificationStreamService.Subscribe(UserId);
        try
        {
            var unreadNotifications = await notificationService.GetUnreadAsync(UserId, new NotificationQueryParams());
            foreach (var notification in unreadNotifications)
            {
                await WriteEventAsync(notification, cancellationToken);
            }

            await foreach (var notification in subscription.Reader.ReadAllAsync(cancellationToken))
            {
                await WriteEventAsync(notification, cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            // Client disconnected.
        }
        finally
        {
            notificationStreamService.Unsubscribe(UserId, subscription.ConnectionId);
        }
    }

    private async Task WriteEventAsync(NotificationDto notification, CancellationToken cancellationToken)
    {
        var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        var json = JsonSerializer.Serialize(notification, options);
        var payload = $"data: {json}\n\n";
        var bytes = Encoding.UTF8.GetBytes(payload);
        await Response.Body.WriteAsync(bytes, cancellationToken);
        await Response.Body.FlushAsync(cancellationToken);
    }
}