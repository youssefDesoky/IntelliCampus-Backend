using IntelliCampus.Domain.Entities;
using IntelliCampus.Domain.Entities.Enums;
using IntelliCampus.Domain.Interfaces;
using IntelliCampus.Service.Exceptions;
using IntelliCampus.Service.Specifications;
using IntelliCampus.Service_Abstraction;
using IntelliCampus.Shared.Dtos.Reminder;
using IntelliCampus.Shared.Params;

namespace IntelliCampus.Service;

public class InstructorReminderService(IUnitOfWork unitOfWork) : IInstructorReminderService
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;

    private IGenericRepository<Reminder, int> Reminders
        => _unitOfWork.GetRepository<Reminder, int>();

    private IGenericRepository<Instructor, int> Instructors
        => _unitOfWork.GetRepository<Instructor, int>();

    public async Task<RemindersGroupedDto> GetRemindersAsync(int instructorId, ReminderQueryParams queryParams)
    {
        var selectedDay = queryParams.SelectedDay ?? DateOnly.FromDateTime(DateTime.UtcNow);

        var instructorExists = await Instructors.GetByIdAsync(instructorId) != null;
        if (!instructorExists)
            throw new InstructorNotFoundException(instructorId);

        var spec = new RemindersByInstructorSpec(instructorId, queryParams);
        var reminders = await Reminders.GetAllAsync(spec, asNoTracking: true);

        var nextDay = selectedDay.AddDays(1);
        var weekEnd = selectedDay.AddDays(8);

        var selectedDayItems = reminders
            .Where(r => DateOnly.FromDateTime(r.Date) == selectedDay)
            .OrderBy(r => r.Date)
            .Select(MapToDto)
            .ToList();

        var nextDayItems = reminders
            .Where(r => DateOnly.FromDateTime(r.Date) == nextDay)
            .OrderBy(r => r.Date)
            .Select(MapToDto)
            .ToList();

        var weekItems = reminders
            .Where(r => DateOnly.FromDateTime(r.Date) >= selectedDay && DateOnly.FromDateTime(r.Date) < weekEnd)
            .OrderBy(r => r.Date)
            .Select(MapToDto)
            .ToList();

        return new RemindersGroupedDto
        {
            SelectedDay = selectedDayItems,
            NextDay = nextDayItems,
            Week = weekItems
        };
    }

    public async Task<ReminderDto> CreatePersonalReminderAsync(int instructorId, CreateReminderDto dto)
    {
        var instructorExists = await Instructors.GetByIdAsync(instructorId) != null;
        if (!instructorExists)
            throw new InstructorNotFoundException(instructorId);

        var entity = new Reminder
        {
            InstructorId = instructorId,
            Title = dto.Title,
            Date = dto.DueAt,
            Type = ReminderType.Custom,
            Location = dto.Location,
            Priority = string.IsNullOrWhiteSpace(dto.Priority) ? "low" : dto.Priority
        };

        Reminders.Add(entity);
        await _unitOfWork.SaveChangesAsync();

        return MapToDto(entity);
    }

    public async Task<ReminderDto?> UpdatePersonalReminderAsync(int instructorId, string reminderId, UpdateReminderDto dto)
    {
        if (!int.TryParse(reminderId, out var id))
            throw new InvalidOperationException("Invalid reminder ID.");

        var spec = new ReminderByIdSpec(id);
        var reminder = await Reminders.GetByIdAsync(spec);

        if (reminder is null || reminder.InstructorId != instructorId)
            throw new ReminderNotFoundException(id);

        reminder.Title = dto.Title;
        reminder.Date = dto.DueAt;
        reminder.Type = ReminderType.Custom;
        reminder.Location = dto.Location;
        reminder.Priority = string.IsNullOrWhiteSpace(dto.Priority) ? "low" : dto.Priority;
        reminder.State = Enum.TryParse<SubmissionState>(dto.SubmissionState, ignoreCase: true, out var parsed) ? parsed : SubmissionState.Unsubmitted;

        Reminders.Update(reminder);
        await _unitOfWork.SaveChangesAsync();

        return MapToDto(reminder);
    }

    public async Task<bool> DeletePersonalReminderAsync(int instructorId, string reminderId)
    {
        if (!int.TryParse(reminderId, out var id))
            throw new InvalidOperationException("Invalid reminder ID.");

        var spec = new ReminderByIdSpec(id);
        var reminder = await Reminders.GetByIdAsync(spec);

        if (reminder is null || reminder.InstructorId != instructorId)
            throw new ReminderNotFoundException(id);

        Reminders.Delete(reminder);
        await _unitOfWork.SaveChangesAsync();

        return true;
    }

    private static ReminderDto MapToDto(Reminder reminder)
    {
        var category = reminder.Type switch
        {
            ReminderType.Assignment => "assignments",
            ReminderType.Quiz => "quizzes",
            ReminderType.Exam => "exams",
            ReminderType.Class => "classes",
            _ => "personal"
        };

        return new ReminderDto
        {
            Id = reminder.ReminderId.ToString(),
            Title = reminder.Title,
            DueAt = reminder.Date,
            Location = reminder.Location ?? string.Empty,
            Category = category,
            Priority = reminder.Priority ?? "low",
            SubmissionState = reminder.State.ToString().ToLowerInvariant()
        };
    }
}
