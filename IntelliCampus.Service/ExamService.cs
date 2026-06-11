using IntelliCampus.Domain.Entities;
using IntelliCampus.Domain.Entities.Enums;
using IntelliCampus.Domain.Interfaces;
using IntelliCampus.Service.Specifications;
using IntelliCampus.Service_Abstraction;
using IntelliCampus.Shared.Dtos.Exam;

namespace IntelliCampus.Service;

public class ExamService : IExamService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IExamScheduleService _examScheduleService;
    private readonly INotificationService _notificationService;

    public ExamService(
        IUnitOfWork unitOfWork,
        IExamScheduleService examScheduleService,
        INotificationService notificationService)
    {
        _unitOfWork = unitOfWork;
        _examScheduleService = examScheduleService;
        _notificationService = notificationService;
    }

    private IGenericRepository<Exam, int> Exams
        => _unitOfWork.GetRepository<Exam, int>();
    private IGenericRepository<Reminder, int> RemindersRepo
        => _unitOfWork.GetRepository<Reminder, int>();
    private IGenericRepository<Class, int> ClassesRepo
        => _unitOfWork.GetRepository<Class, int>();

    public async Task<ExamDto?> GetByIdAsync(int examId)
    {
        var spec = new ExamWithDetailsSpec(examId);
        var exam = await Exams.GetByIdAsync(spec);
        return exam is null ? null : MapToDto(exam);
    }

    public async Task<IEnumerable<ExamDto>> GetAllAsync()
    {
        var spec = new ExamWithDetailsSpec();
        var exams = await Exams.GetAllAsync(spec);
        return exams.Select(MapToDto);
    }

    public async Task<IEnumerable<ExamDto>> GetByCourseIdAsync(int courseId)
    {
        var spec = new ExamWithDetailsSpec();
        var all = await Exams.GetAllAsync(spec);
        return all.Where(e => e.CourseId == courseId).Select(MapToDto);
    }

    public async Task<ExamDto> CreateAsync(CreateExamDto dto)
    {
        var exam = new Exam
        {
            Title = dto.Title,
            Description = dto.Description,
            ExamType = dto.ExamType,
            Status = dto.Date > DateTime.UtcNow ? ExamStatus.Upcoming : ExamStatus.Completed,
            Date = dto.Date,
            Time = dto.Time,
            DurationMinutes = dto.DurationMinutes,
            MaxGrade = dto.MaxGrade,
            TotalMarks = dto.TotalMarks,
            RoomId = dto.RoomId,
            CourseId = dto.CourseId,
            CreatedAt = DateTime.UtcNow
        };

        Exams.Add(exam);
        await _unitOfWork.SaveChangesAsync();

        await _examScheduleService.SyncFromExamAsync(exam.ExamId);
        await SendExamNotificationsAsync(exam);

        return MapToDto(exam);
    }

    public async Task<ExamDto?> UpdateAsync(int examId, UpdateExamDto dto)
    {
        var spec = new ExamWithDetailsSpec(examId);
        var exam = await Exams.GetByIdAsync(spec);

        if (exam is null)
            return null;

        if (dto.Title is not null)
            exam.Title = dto.Title;

        if (dto.Description is not null)
            exam.Description = dto.Description;

        if (dto.ExamType.HasValue)
            exam.ExamType = dto.ExamType.Value;

        if (dto.Status.HasValue)
            exam.Status = dto.Status.Value;

        if (dto.Date.HasValue)
        {
            exam.Date = dto.Date.Value;
            exam.Status = exam.Date > DateTime.UtcNow ? ExamStatus.Upcoming : ExamStatus.Completed;
        }

        if (dto.Time.HasValue)
            exam.Time = dto.Time.Value;

        if (dto.DurationMinutes.HasValue)
            exam.DurationMinutes = dto.DurationMinutes.Value;

        if (dto.MaxGrade.HasValue)
            exam.MaxGrade = dto.MaxGrade.Value;

        if (dto.TotalMarks.HasValue)
            exam.TotalMarks = dto.TotalMarks;

        if (dto.RoomId.HasValue)
            exam.RoomId = dto.RoomId;

        if (dto.CourseId.HasValue)
            exam.CourseId = dto.CourseId.Value;

        Exams.Update(exam);
        await _unitOfWork.SaveChangesAsync();

        await _examScheduleService.SyncFromExamAsync(exam.ExamId);
        await SendExamNotificationsAsync(exam);

        return MapToDto(exam);
    }

    public async Task<bool> DeleteAsync(int examId)
    {
        var exam = await Exams.GetByIdAsync(examId);

        if (exam is null)
            return false;

        await _examScheduleService.RemoveByExamAsync(examId);

        Exams.Delete(exam);
        await _unitOfWork.SaveChangesAsync();

        return true;
    }

    private async Task SendExamNotificationsAsync(Exam exam)
    {
        var roomName = exam.Room?.RoomName;
        var location = roomName is not null ? $" in {roomName}" : "";

        // Student notification message
        var studentMessage = "Exams schedule posted, Good Luck!";

        // Instructor notification message
        var instructorMessage = $"Exam schedule posted: {exam.Title} on {exam.Date:dd MMM yyyy} at {exam.Time:hh\\:mm}{location}";

        // --- Notify Students ---
        var studentIds = exam.Course.StudentCourses
            .Select(sc => sc.StudentId)
            .Distinct()
            .ToList();

        if (studentIds.Count > 0)
        {
            // Notifications
            await _notificationService.SendToManyAsync(
                studentIds,
                NotificationType.ScheduleUpdated,
                studentMessage);

            // High-priority reminders
            foreach (var studentId in studentIds)
            {
                RemindersRepo.Add(new Reminder
                {
                    StudentId = studentId,
                    Title = $"Exam: {exam.Title} on {exam.Date:dd MMM yyyy} at {exam.Time:hh\\:mm}",
                    Date = exam.Date,
                    Type = ReminderType.Exam,
                    Location = roomName,
                    Priority = "high"
                });
            }

            await _unitOfWork.SaveChangesAsync();
        }

        // --- Notify Instructors ---
        var allClasses = await ClassesRepo.GetAllAsync();
        var instructorIds = allClasses
            .Where(c => c.CourseId == exam.CourseId && c.InstructorId.HasValue)
            .Select(c => c.InstructorId!.Value)
            .Distinct()
            .ToList();

        if (instructorIds.Count > 0)
        {
            await _notificationService.SendToManyAsync(
                instructorIds,
                NotificationType.ScheduleUpdated,
                instructorMessage);
        }
    }

    private static ExamDto MapToDto(Exam exam)
    {
        return new ExamDto
        {
            ExamId = exam.ExamId,
            Title = exam.Title,
            Description = exam.Description,
            ExamType = exam.ExamType,
            Status = exam.Status,
            Date = exam.Date,
            Time = exam.Time,
            DurationMinutes = exam.DurationMinutes,
            MaxGrade = exam.MaxGrade,
            TotalMarks = exam.TotalMarks,
            RoomId = exam.RoomId,
            RoomName = exam.Room?.RoomName,
            CourseId = exam.CourseId,
            CourseName = exam.Course?.CourseName,
            CourseCode = exam.Course?.CourseCode,
            CreatedAt = exam.CreatedAt
        };
    }
}
