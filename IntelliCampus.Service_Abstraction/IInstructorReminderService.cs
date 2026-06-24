using IntelliCampus.Shared.Dtos.Reminder;
using IntelliCampus.Shared.Params;

namespace IntelliCampus.Service_Abstraction;

public interface IInstructorReminderService
{
    Task<RemindersGroupedDto> GetRemindersAsync(int instructorId, ReminderQueryParams queryParams);
    Task<ReminderDto> CreatePersonalReminderAsync(int instructorId, CreateReminderDto dto);
    Task<ReminderDto?> UpdatePersonalReminderAsync(int instructorId, string reminderId, UpdateReminderDto dto);
    Task<bool> DeletePersonalReminderAsync(int instructorId, string reminderId);
}
