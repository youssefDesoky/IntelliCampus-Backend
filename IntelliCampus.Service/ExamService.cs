using IntelliCampus.Domain.Entities;
using IntelliCampus.Domain.Entities.Enums;
using IntelliCampus.Domain.Helpers;
using IntelliCampus.Domain.Interfaces;
using IntelliCampus.Service.Specifications;
using IntelliCampus.Service_Abstraction;
using IntelliCampus.shared.Pagination;
using IntelliCampus.Shared.Dtos.Exam;
using IntelliCampus.Shared.Params;
using IntelliCampus.Service.Exceptions;

namespace IntelliCampus.Service;

public class ExamService : IExamService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IExamScheduleService _examScheduleService;
    private readonly INotificationService _notificationService;
    private readonly ICurrentAdminContext _adminContext;

    public ExamService(
        IUnitOfWork unitOfWork,
        IExamScheduleService examScheduleService,
        INotificationService notificationService,
        ICurrentAdminContext adminContext)
    {
        _unitOfWork = unitOfWork;
        _examScheduleService = examScheduleService;
        _notificationService = notificationService;
        _adminContext = adminContext;
    }

    private IGenericRepository<Exam, int> Exams
        => _unitOfWork.GetRepository<Exam, int>();
    private IGenericRepository<Reminder, int> RemindersRepo
        => _unitOfWork.GetRepository<Reminder, int>();
    private IGenericRepository<Class, int> ClassesRepo
        => _unitOfWork.GetRepository<Class, int>();
    private IGenericRepository<StudentCourse, (int, int)> StudentCourseRepo
        => _unitOfWork.GetRepository<StudentCourse, (int, int)>();
    private IGenericRepository<Course, int> CoursesRepo
        => _unitOfWork.GetRepository<Course, int>();

    public async Task<ExamDto?> GetByIdAsync(int examId)
    {
        var spec = new ExamWithDetailsSpec(examId);
        var exam = await Exams.GetByIdAsync(spec);
        if (exam is null)
            throw new ExamNotFoundException(examId);

        if (_adminContext.IsAdmin)
            await _adminContext.EnsureCanAccessExamAsync(examId);

        return MapToDto(exam);
    }

    public async Task<PaginatedResult<ExamDto>> GetAllAsync(ExamQueryParams queryParams)
    {
        if (_adminContext.IsAdmin)
            queryParams.FacultyId = await _adminContext.GetFacultyIdAsync();

        var spec = new ExamWithCourseSpec(queryParams);
        var exams = await Exams.GetAllAsync(spec, asNoTracking: true);
        var dataToReturn = exams.Select(MapToDto).ToList();

        var countSpec = new ExamCountSpec(queryParams);
        var totalCount = await Exams.CountAsync(countSpec);

        return new PaginatedResult<ExamDto>(queryParams.PageIndex, dataToReturn.Count, totalCount, dataToReturn);
    }

    public async Task<IEnumerable<ExamDto>> GetByCourseIdAsync(int courseId)
    {
        var course = await CoursesRepo.GetByIdAsync(courseId);
        if (course is null)
            throw new CourseNotFoundException(courseId);

        if (_adminContext.IsAdmin)
            await _adminContext.EnsureCanAccessCourseAsync(courseId);

        var spec = new ExamWithDetailsSpec(courseId, filterByCourse: true);
        var exams = await Exams.GetAllAsync(spec, asNoTracking: true);
        return exams.Select(MapToDto);
    }

    public async Task<ExamDto> CreateAsync(CreateExamDto dto)
    {
        await _adminContext.EnsureAdminHasFacultyAsync();

        var course = await CoursesRepo.GetByIdAsync(dto.CourseId);
        if (course is null)
            throw new CourseNotFoundException(dto.CourseId);

        var conflicts = await GetConflictsAsync(dto.CourseId, dto.Date, dto.Time, dto.Time.Add(TimeSpan.FromMinutes(dto.DurationMinutes)));
        if (conflicts.Count > 0)
            throw new InvalidOperationException($"Schedule conflict detected: {conflicts.Count} student(s) have overlapping exams.");

        var now = EgyptTime.Now;
        var exam = new Exam
        {
            Title = dto.Title,
            Description = dto.Description,
            ExamType = dto.ExamType,
            Status = dto.Date > now ? ExamStatus.Upcoming : ExamStatus.Completed,
            Date = dto.Date,
            Time = dto.Time,
            DurationMinutes = dto.DurationMinutes,
            MaxGrade = dto.MaxGrade,
            TotalMarks = dto.TotalMarks,
            RoomId = dto.RoomId,
            CourseId = dto.CourseId,
            CreatedAt = now
        };

        Exams.Add(exam);
        await _unitOfWork.SaveChangesAsync();

        await _examScheduleService.SyncFromExamAsync(exam.ExamId);

        var examWithDetails = await Exams.GetByIdAsync(new ExamWithDetailsSpec(exam.ExamId));
        await SendExamNotificationsAsync(examWithDetails!);

        return MapToDto(exam);
    }

    public async Task<ExamDto?> UpdateAsync(int examId, UpdateExamDto dto)
    {
        var spec = new ExamWithDetailsSpec(examId);
        var exam = await Exams.GetByIdAsync(spec);

        if (exam is null)
            throw new ExamNotFoundException(examId);

        await _adminContext.EnsureCanAccessExamAsync(examId);

        var effectiveCourseId = dto.CourseId ?? exam.CourseId;
        var effectiveDate = dto.Date ?? exam.Date;
        var effectiveTime = dto.Time ?? exam.Time;
        var effectiveDuration = dto.DurationMinutes ?? exam.DurationMinutes;

        if (dto.Date.HasValue || dto.Time.HasValue || dto.DurationMinutes.HasValue || dto.CourseId.HasValue)
        {
            var endTime = effectiveTime.Add(TimeSpan.FromMinutes(effectiveDuration));
            var conflicts = await GetConflictsAsync(effectiveCourseId, effectiveDate, effectiveTime, endTime, excludeExamId: examId);
            if (conflicts.Count > 0)
                throw new InvalidOperationException($"Schedule conflict detected: {conflicts.Count} student(s) have overlapping exams.");
        }

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
            var now = EgyptTime.Now;
            exam.Status = exam.Date > now ? ExamStatus.Upcoming : ExamStatus.Completed;
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
            throw new ExamNotFoundException(examId);

        await _adminContext.EnsureCanAccessExamAsync(examId);

        await _examScheduleService.RemoveByExamAsync(examId);

        Exams.Delete(exam);
        await _unitOfWork.SaveChangesAsync();

        return true;
    }

    private async Task<List<int>> GetConflictsAsync(int courseId, DateTime date, TimeSpan startTime, TimeSpan endTime, int? excludeExamId = null)
    {
        var semester = SemesterHelper.GetSemesterFromDate(date);
        var enrolledStudentIds = (await StudentCourseRepo.GetAllAsync(
            new StudentCourseIdsSpec(courseId, true, StudentCourseStatus.InProgress), asNoTracking: true))
            .Where(sc => sc.Semester == semester)
            .Select(sc => sc.StudentId)
            .ToHashSet();

        if (enrolledStudentIds.Count == 0)
            return [];

        var allStudentCourses = (await StudentCourseRepo.GetAllAsync(
            new StudentCourseSemesterAllSpec(semester), asNoTracking: true))
            .Where(sc => enrolledStudentIds.Contains(sc.StudentId))
            .ToList();

        var otherEnrolledCourseIds = allStudentCourses
            .Where(sc => enrolledStudentIds.Contains(sc.StudentId) && sc.CourseId != courseId && sc.Semester == semester)
            .Select(sc => sc.CourseId)
            .ToHashSet();

        var overlappingExams = (await Exams.GetAllAsync(
            new ExamByDateSpec(date, excludeExamId), asNoTracking: true))
            .Where(ex =>
                ex.Time < endTime &&
                ex.Time.Add(TimeSpan.FromMinutes(ex.DurationMinutes)) > startTime &&
                otherEnrolledCourseIds.Contains(ex.CourseId))
            .ToList();

        if (overlappingExams.Count == 0)
            return [];

        var conflictingStudentIds = allStudentCourses
            .Where(sc =>
                overlappingExams.Select(e => e.CourseId).Contains(sc.CourseId) &&
                enrolledStudentIds.Contains(sc.StudentId))
            .Select(sc => sc.StudentId)
            .Distinct()
            .ToList();

        return conflictingStudentIds;
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
            .Where(sc => sc.Status == StudentCourseStatus.InProgress)
            .Select(sc => sc.StudentId)
            .Distinct()
            .ToList();

        if (studentIds.Count > 0)
        {
            // Notifications
            await _notificationService.SendToManyAsync(
                studentIds,
                NotificationType.ScheduleUpdated,
                studentMessage,
                clickUrl: $"/courses/{exam.CourseId}/exams/{exam.ExamId}");

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

        var instructorIds = (await ClassesRepo.GetAllAsync(
            new ClassByCourseSpec(exam.CourseId), asNoTracking: true))
            .Where(c => c.InstructorId.HasValue)
            .Select(c => c.InstructorId!.Value)
            .Distinct()
            .ToList();

        if (instructorIds.Count > 0)
        {
            await _notificationService.SendToManyAsync(
                instructorIds,
                NotificationType.ScheduleUpdated,
                instructorMessage,
                clickUrl: $"/courses/{exam.CourseId}/exams/{exam.ExamId}");
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
            RoomNameAr = exam.Room?.RoomNameAr,
            CourseId = exam.CourseId,
            CourseName = exam.Course?.CourseName,
            CourseNameAr = exam.Course?.CourseNameAr,
            CourseCode = exam.Course?.CourseCode,
            CourseCodeAr = exam.Course?.CourseCodeAr,
            CreatedAt = exam.CreatedAt
        };
    }
}