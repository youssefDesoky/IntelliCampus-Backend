using IntelliCampus.Domain.Entities;
using IntelliCampus.Domain.Entities.Enums;
using IntelliCampus.Domain.Interfaces;
using IntelliCampus.Service.Specifications;
using IntelliCampus.Service_Abstraction;
using IntelliCampus.Shared.Dtos.Schedule;

namespace IntelliCampus.Service;

public class ExamScheduleService : IExamScheduleService
{
    private readonly IUnitOfWork _unitOfWork;

    public ExamScheduleService(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    private IGenericRepository<ExamSchedule, int> ExamSchedules
        => _unitOfWork.GetRepository<ExamSchedule, int>();

    private IGenericRepository<Exam, int> Exams
        => _unitOfWork.GetRepository<Exam, int>();

    public async Task<ExamScheduleDto?> GetByIdAsync(int examScheduleId)
    {
        var spec = new ExamScheduleSpec(examScheduleId, byId: true);
        var exam = await ExamSchedules.GetByIdAsync(spec);
        return exam is null ? null : MapToDto(exam);
    }

    public async Task<IEnumerable<ExamScheduleDto>> GetByStudentIdAsync(int studentId)
    {
        var spec = new ExamScheduleSpec(studentId);
        var exams = await ExamSchedules.GetAllAsync(spec);
        return exams.Select(MapToDto);
    }

    public async Task<IEnumerable<ExamScheduleDto>> GetByTypeAsync(int studentId, ExamType examType)
    {
        var spec = new ExamScheduleSpec(studentId, examType);
        var exams = await ExamSchedules.GetAllAsync(spec);
        return exams.Select(MapToDto);
    }

    public async Task<IEnumerable<ExamScheduleDto>> GetByStatusAsync(int studentId, ExamStatus status)
    {
        var spec = new ExamScheduleSpec(studentId, status);
        var exams = await ExamSchedules.GetAllAsync(spec);
        return exams.Select(MapToDto);
    }

    public async Task SyncFromExamAsync(int examId)
    {
        var exam = await Exams.GetByIdAsync(new ExamWithCourseSpec(examId));
        if (exam is null)
            throw new InvalidOperationException("Exam not found.");

        await RemoveByExamAsync(examId);

        // Current domain model: Exam has only Date + Time (no StartTime/EndTime), so we derive.
        var startTime = exam.Time;
        var endTime = startTime.Add(TimeSpan.FromHours(2));

        var duration = endTime - startTime;
        var durationText = duration.TotalMinutes % 60 == 0
            ? $"{(int)duration.TotalHours} hour{((int)duration.TotalHours == 1 ? "" : "s")}" 
            : $"{duration.TotalHours:0.#} hours";

        var status = exam.Date > DateTime.UtcNow ? ExamStatus.Upcoming : ExamStatus.Completed;

        // Current domain model: Exam does not store exam type; default to Midterm.
        // (If you add ExamType to Exam later, update this to use it.)
        const ExamType examType = ExamType.Midterm;

        foreach (var sc in exam.Course.StudentCourses)
        {
            var entry = new ExamSchedule
            {
                CourseCode = exam.Course.CourseCode ?? string.Empty,
                CourseName = exam.Course.CourseName,
                Day = exam.Date.DayOfWeek.ToString(),
                Date = exam.Date,
                StartTime = startTime,
                EndTime = endTime,
                Duration = durationText,
                Location = exam.Location,
                ExamType = examType,
                Status = status,
                StudentId = sc.StudentId,
                ExamId = exam.ExamId
            };

            ExamSchedules.Add(entry);
        }

        await _unitOfWork.SaveChangesAsync();
    }

    public async Task RemoveByExamAsync(int examId)
    {
        var schedules = await ExamSchedules.GetAllAsync(new ExamScheduleByExamIdSpec(examId));
        foreach (var s in schedules)
            ExamSchedules.Delete(s);

        await _unitOfWork.SaveChangesAsync();
    }

    private static ExamScheduleDto MapToDto(ExamSchedule e) => new()
    {
        ExamScheduleId = e.ExamScheduleId,
        CourseCode = e.CourseCode,
        CourseName = e.CourseName,
        Day = e.Day,
        Date = e.Date,
        StartTime = e.StartTime,
        EndTime = e.EndTime,
        Duration = e.Duration,
        Location = e.Location,
        ExamType = e.ExamType,
        Status = e.Status,
        StudentId = e.StudentId
    };
}
