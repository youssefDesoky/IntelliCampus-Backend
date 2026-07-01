using System.Security.Claims;
using IntelliCampus.Domain.Entities;
using IntelliCampus.Domain.Interfaces;
using IntelliCampus.Shared.Dtos.Notification;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IntelliCampus.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class NotificationPreferencesController(IUnitOfWork unitOfWork) : ControllerBase
{
    private int UserId
        => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet]
    public async Task<ActionResult<NotificationPreferenceDto>> Get()
    {
        var repo = unitOfWork.GetRepository<UserNotificationSettings, int>();
        var allSettings = await repo.GetAllAsync();
        var settings = allSettings.FirstOrDefault(s => s.UserId == UserId);

        if (settings is null)
            return Ok(new NotificationPreferenceDto
            {
                InAppNotificationsEnabled = true,
                PushNotificationsEnabled = false
            });

        return Ok(new NotificationPreferenceDto
        {
            InAppNotificationsEnabled = settings.InAppNotificationsEnabled,
            PushNotificationsEnabled = settings.PushNotificationsEnabled
        });
    }

    [HttpPut]
    public async Task<IActionResult> Update([FromBody] NotificationPreferenceDto dto)
    {
        var repo = unitOfWork.GetRepository<UserNotificationSettings, int>();
        var allSettings = await repo.GetAllAsync();
        var settings = allSettings.FirstOrDefault(s => s.UserId == UserId);

        if (settings is null)
        {
            repo.Add(new UserNotificationSettings
            {
                UserId = UserId,
                InAppNotificationsEnabled = dto.InAppNotificationsEnabled,
                PushNotificationsEnabled = dto.PushNotificationsEnabled
            });
        }
        else
        {
            settings.InAppNotificationsEnabled = dto.InAppNotificationsEnabled;
            settings.PushNotificationsEnabled = dto.PushNotificationsEnabled;
            repo.Update(settings);
        }

        await unitOfWork.SaveChangesAsync();
        return Ok();
    }
}
