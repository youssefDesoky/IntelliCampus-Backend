using IntelliCampus.Domain.Entities;
using IntelliCampus.Domain.Entities.Enums;
using IntelliCampus.Domain.Interfaces;
using IntelliCampus.Service.Specifications;
using IntelliCampus.Service_Abstraction;
using IntelliCampus.Shared.Dtos.Export;
using IntelliCampus.Shared.Dtos.Schedule;

namespace IntelliCampus.Service;

public class ExamScheduleService : IExamScheduleService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IStudentService _studentService;
    private readonly IPdfExportService _pdfExportService;

    public ExamScheduleService(IUnitOfWork unitOfWork, IStudentService studentService, IPdfExportService pdfExportService)
    {
        _unitOfWork = unitOfWork;
        _studentService = studentService;
        _pdfExportService = pdfExportService;
    }

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
        var spec = new ExamWithDetailsSpec(examId);
        var exam = await Exams.GetByIdAsync(spec);
        if (exam is null)
            throw new InvalidOperationException("Exam not found.");

        await RemoveByExamAsync(examId);

        var startTime = exam.Time;
        var endTime = startTime.Add(TimeSpan.FromMinutes(exam.DurationMinutes));

        var duration = endTime - startTime;
        var durationText = duration.TotalMinutes % 60 == 0
            ? $"{(int)duration.TotalHours} hour{((int)duration.TotalHours == 1 ? "" : "s")}" 
            : $"{duration.TotalHours:0.#} hours";

        var status = exam.Status;

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
                Location = exam.Room?.RoomName,
                ExamType = exam.ExamType,
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

    public async Task<byte[]> ExportExamSchedulePdfAsync(int studentId, ExamType? type, ExamStatus? status)
    {
        var student = await _studentService.GetByIdAsync(studentId);

        IEnumerable<ExamScheduleDto> exams;
        if (type.HasValue)
            exams = await GetByTypeAsync(studentId, type.Value);
        else if (status.HasValue)
            exams = await GetByStatusAsync(studentId, status.Value);
        else
            exams = await GetByStudentIdAsync(studentId);

        var dto = new ExamScheduleExportDto
        {
            StudentName = student?.FullName ?? "",
            StudentCode = student?.StudentCode ?? "-",
            Title = "Exam Schedule",
            Items = exams.Select(e => new ExamScheduleItem
            {
                CourseCode = e.CourseCode,
                CourseName = e.CourseName,
                Day = ToFullDayName(e.Day),
                Date = e.Date.ToString("dd MMM yyyy"),
                StartTime = e.StartTime,
                EndTime = e.EndTime,
                Duration = e.Duration,
                Location = e.Location,
                ExamType = e.ExamType.ToString()
            }).ToList()
        };

        return _pdfExportService.ExportExamSchedule(dto);
    }

    private static ExamScheduleDto MapToDto(ExamSchedule e) => new()
    {
        ExamScheduleId = e.ExamScheduleId,
        CourseCode = e.CourseCode,
        CourseName = e.CourseName,
        Day = ToDayAbbreviation(e.Day),
        Date = e.Date,
        StartTime = FormatTime(e.StartTime),
        EndTime = FormatTime(e.EndTime),
        Duration = e.Duration,
        Location = e.Location,
        ExamType = e.ExamType,
        Status = e.Status,
        StudentId = e.StudentId
    };

    private static string ToDayAbbreviation(string day) => day?.ToLowerInvariant() switch
    {
        "saturday" or "sat" => "sat",
        "sunday" or "sun" => "sun",
        "monday" or "mon" => "mon",
        "tuesday" or "tue" => "tue",
        "wednesday" or "wed" => "wed",
        "thursday" or "thu" => "thu",
        "friday" or "fri" => "fri",
        _ => day?.ToLowerInvariant() ?? string.Empty
    };

    private static string ToFullDayName(string day) => day?.ToLowerInvariant() switch
    {
        "sat" or "saturday" => "Saturday",
        "sun" or "sunday" => "Sunday",
        "mon" or "monday" => "Monday",
        "tue" or "tuesday" => "Tuesday",
        "wed" or "wednesday" => "Wednesday",
        "thu" or "thursday" => "Thursday",
        "fri" or "friday" => "Friday",
        _ => day ?? string.Empty
    };

    private static string FormatTime(TimeSpan time) =>
        DateTime.Today.Add(time).ToString("hh:mm tt");
}
