using IntelliCampus.Shared.Dtos.Reminder;

namespace IntelliCampus.Service_Abstraction;

public interface IInstructorReminderService
{
    Task<RemindersGroupedDto> GetRemindersAsync(int instructorId, DateOnly selectedDay);
    Task<ReminderDto> CreatePersonalReminderAsync(int instructorId, CreateReminderDto dto);
    Task<ReminderDto?> UpdatePersonalReminderAsync(int instructorId, string reminderId, UpdateReminderDto dto);
    Task<bool> DeletePersonalReminderAsync(int instructorId, string reminderId);
}
