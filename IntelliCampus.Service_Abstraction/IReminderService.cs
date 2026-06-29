using IntelliCampus.Domain.Entities.Enums;
using IntelliCampus.Shared.Dtos.Reminder;
using IntelliCampus.Shared.Params;

namespace IntelliCampus.Service_Abstraction;

public interface IReminderService
{
    Task<RemindersGroupedDto> GetRemindersAsync(int studentId, ReminderQueryParams queryParams);

    Task<ReminderDto> CreatePersonalReminderAsync(int studentId, CreateReminderDto dto);

    Task<ReminderDto?> UpdatePersonalReminderAsync(int studentId, string reminderId, UpdateReminderDto dto);

    Task<bool> DeletePersonalReminderAsync(int studentId, string reminderId);

    Task MarkSubmissionCompletedAsync(int studentId, ReminderType type, DateTime date);
}
