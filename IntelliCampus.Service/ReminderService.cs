using IntelliCampus.Domain.Entities;
using IntelliCampus.Domain.Entities.Enums;
using IntelliCampus.Domain.Interfaces;
using IntelliCampus.Service.Exceptions;
using IntelliCampus.Service.Specifications;
using IntelliCampus.Service_Abstraction;
using IntelliCampus.Shared.Dtos.Reminder;
using IntelliCampus.Shared.Params;

namespace IntelliCampus.Service;

public class ReminderService(IUnitOfWork unitOfWork) : IReminderService
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;

    private IGenericRepository<Reminder, int> Reminders
        => _unitOfWork.GetRepository<Reminder, int>();

    private IGenericRepository<Student, int> Students
        => _unitOfWork.GetRepository<Student, int>();

    public async Task<RemindersGroupedDto> GetRemindersAsync(int studentId, ReminderQueryParams queryParams)
    {
        var selectedDay = queryParams.SelectedDay ?? DateOnly.FromDateTime(DateTime.UtcNow);

        var studentExists = await Students.GetByIdAsync(studentId) != null;
        if (!studentExists)
            throw new StudentNotFoundException(studentId);

        var spec = new RemindersByStudentSpec(studentId, queryParams);
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

    public async Task<ReminderDto> CreatePersonalReminderAsync(int studentId, CreateReminderDto dto)
    {
        var studentExists = await Students.GetByIdAsync(studentId) != null;
        if (!studentExists)
            throw new StudentNotFoundException(studentId);

        var entity = new Reminder
        {
            StudentId = studentId,
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

    public async Task<ReminderDto?> UpdatePersonalReminderAsync(int studentId, string reminderId, UpdateReminderDto dto)
    {
        if (!int.TryParse(reminderId, out var id))
            throw new InvalidOperationException("Invalid reminder ID.");

        var spec = new ReminderByIdSpec(id);
        var reminder = await Reminders.GetByIdAsync(spec);

        if (reminder is null || reminder.StudentId != studentId)
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

    public async Task<bool> DeletePersonalReminderAsync(int studentId, string reminderId)
    {
        if (!int.TryParse(reminderId, out var id))
            throw new InvalidOperationException("Invalid reminder ID.");

        var spec = new ReminderByIdSpec(id);
        var reminder = await Reminders.GetByIdAsync(spec);

        if (reminder is null || reminder.StudentId != studentId)
            throw new ReminderNotFoundException(id);

        Reminders.Delete(reminder);
        await _unitOfWork.SaveChangesAsync();

        return true;
    }

    public async Task MarkSubmissionCompletedAsync(int studentId, ReminderType type, DateTime date)
    {
        var spec = new RemindersByStudentSpec(studentId, new ReminderQueryParams
        {
            SelectedDay = DateOnly.FromDateTime(date)
        });
        var reminders = await Reminders.GetAllAsync(spec);
        foreach (var r in reminders)
        {
            if (r.Type == type && r.Date == date)
            {
                r.State = SubmissionState.Completed;
                Reminders.Update(r);
            }
        }
        await _unitOfWork.SaveChangesAsync();
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
