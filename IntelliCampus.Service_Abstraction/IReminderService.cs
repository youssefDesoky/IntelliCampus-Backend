using IntelliCampus.Shared.Dtos.Reminder;

namespace IntelliCampus.Service_Abstraction;

public interface IReminderService
{
    Task<RemindersGroupedDto> GetRemindersAsync(int studentId, DateOnly selectedDay);

    Task<ReminderDto> CreatePersonalReminderAsync(int studentId, CreateReminderDto dto);

    Task<ReminderDto?> UpdatePersonalReminderAsync(int studentId, string reminderId, UpdateReminderDto dto);

    Task<bool> DeletePersonalReminderAsync(int studentId, string reminderId);
}
