using System.Text;
using System.Text.Json;
using IntelliCampus.Domain.Entities;
using IntelliCampus.Domain.Entities.Enums;
using IntelliCampus.Domain.Interfaces;
using IntelliCampus.Service.Exceptions;
using IntelliCampus.Service.Specifications;
using IntelliCampus.Service_Abstraction;
using IntelliCampus.shared.Dtos.Attendance;

namespace IntelliCampus.Service;

public class AttendanceService : IAttendanceService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly INotificationService _notificationService;

    private const decimal AttendanceThreshold = 75m;
    private const int QrValidSeconds = 15;
    private const int MaxIterations = 4;
    private const int IterationWindowSeconds = 60;

    public AttendanceService(
        IUnitOfWork unitOfWork,
        INotificationService notificationService)
    {
        _unitOfWork = unitOfWork;
        _notificationService = notificationService;
    }

    private IGenericRepository<Attendance, int> Attendances
        => _unitOfWork.GetRepository<Attendance, int>();

    private IGenericRepository<Session, int> Sessions
        => _unitOfWork.GetRepository<Session, int>();

    private IGenericRepository<Class, int> Classes
        => _unitOfWork.GetRepository<Class, int>();

    private IGenericRepository<Student, int> Students
        => _unitOfWork.GetRepository<Student, int>();

    private IGenericRepository<Course, int> Courses
        => _unitOfWork.GetRepository<Course, int>();

    private IGenericRepository<QrToken, int> QrTokens
        => _unitOfWork.GetRepository<QrToken, int>();

    public async Task<QrTokenDto> GenerateQrAsync(int studentId)
    {
        var student = await Students.GetByIdAsync(studentId);
        if (student is null)
            throw new StudentNotFoundException(studentId);

        var iterationSpec = new QrTokenSpec(studentId, countIterations: true);
        var recentTokens = await QrTokens.GetAllAsync(iterationSpec);
        var iteration = recentTokens.Count() + 1;

        if (iteration > MaxIterations)
            throw new InvalidOperationException(
                "QR refresh limit reached. Please tap Reload.");

        var now = DateTime.UtcNow;

        var rawToken = Convert.ToBase64String(
            Encoding.UTF8.GetBytes(
                $"{studentId}:{now.Ticks}:{Guid.NewGuid()}"));

        var payload = new QrPayload
        {
            UserId = studentId,
            Name = student.FullName,
            StudentCode = student.StudentCode ?? studentId.ToString(),
            Timestamp = new DateTimeOffset(now).ToUnixTimeMilliseconds(),
            Token = rawToken
        };

        var payloadJson = JsonSerializer.Serialize(payload);
        var qrPayloadEncoded = Convert.ToBase64String(
            Encoding.UTF8.GetBytes(payloadJson));

        var qrToken = new QrToken
        {
            StudentId = studentId,
            Token = rawToken,
            GeneratedAt = now,
            ExpiresAt = now.AddSeconds(QrValidSeconds)
        };

        QrTokens.Add(qrToken);
        await _unitOfWork.SaveChangesAsync();

        return new QrTokenDto
        {
            QrPayload = qrPayloadEncoded,
            ExpiresAt = qrToken.ExpiresAt,
            ExpiresInSeconds = QrValidSeconds,
            Iteration = iteration,
            IsFinal = iteration >= MaxIterations
        };
    }

    public async Task<AttendanceResultDto> ScanQrAsync(
        int instructorId, ScanQrDto dto)
    {
        var session = await Sessions.GetByIdAsync(dto.SessionId);
        if (session is null)
            throw new SessionNotFoundException("Session not found.");

        var classEntity = await Classes.GetByIdAsync(session.ClassId);
        if (classEntity?.InstructorId != instructorId)
            throw new InvalidOperationException("Not authorized.");

        QrPayload payload;
        try
        {
            var json = Encoding.UTF8.GetString(
                Convert.FromBase64String(dto.QrPayload));
            payload = JsonSerializer.Deserialize<QrPayload>(json)
                ?? throw new Exception("Null payload");
        }
        catch
        {
            throw new InvalidOperationException("Invalid QR code format.");
        }

        var tokenSpec = new QrTokenSpec(payload.Token);
        var qrToken = await QrTokens.GetByIdAsync(tokenSpec);

        if (qrToken is null)
            throw new InvalidOperationException(
                "QR code has expired or is invalid. Ask student to refresh.");

        var alreadyRecorded = await Attendances.AnyAsync(
            a => a.StudentId == payload.UserId
              && a.SessionId == dto.SessionId);

        if (alreadyRecorded)
            throw new InvalidOperationException(
                $"{payload.Name} is already recorded for this session.");

        qrToken.ExpiresAt = DateTime.UtcNow.AddSeconds(-1);
        QrTokens.Update(qrToken);

        var student = await Students.GetByIdAsync(payload.UserId);

        var attendance = new Attendance
        {
            StudentId = payload.UserId,
            SessionId = dto.SessionId,
            Status = dto.Status,
            Date = DateTime.UtcNow
        };

        Attendances.Add(attendance);
        await _unitOfWork.SaveChangesAsync();

        await CheckAndNotifyThresholdAsync(
            payload.UserId,
            classEntity.CourseId,
            classEntity.GroupCode ?? "");

        return new AttendanceResultDto
        {
            StudentName = student?.FullName ?? payload.Name,
            StudentCode = payload.StudentCode,
            Status = dto.Status,
            RecordedAt = attendance.Date,
            Method = "QR"
        };
    }

    public async Task<AttendanceResultDto> RecordManualAsync(
        int instructorId, ManualAttendanceDto dto)
    {
        var session = await Sessions.GetByIdAsync(dto.SessionId);
        if (session is null)
            throw new SessionNotFoundException("Session not found.");

        var classEntity = await Classes.GetByIdAsync(session.ClassId);
        if (classEntity?.InstructorId != instructorId)
            throw new InvalidOperationException("Not authorized.");

        var allStudents = await Students.GetAllAsync();
        var student = allStudents.FirstOrDefault(s => s.StudentCode == dto.StudentCode);
        if (student is null)
            throw new InvalidOperationException($"Student with code '{dto.StudentCode}' not found.");

        var alreadyRecorded = await Attendances.AnyAsync(
            a => a.StudentId == student.UserId
              && a.SessionId == dto.SessionId);

        if (alreadyRecorded)
            throw new InvalidOperationException(
                $"{student.FullName} is already recorded for this session.");

        var attendance = new Attendance
        {
            StudentId = student.UserId,
            SessionId = dto.SessionId,
            Status = dto.Status,
            Date = DateTime.UtcNow
        };

        Attendances.Add(attendance);
        await _unitOfWork.SaveChangesAsync();

        await CheckAndNotifyThresholdAsync(
            student.UserId,
            classEntity.CourseId,
            classEntity.GroupCode ?? "");

        return new AttendanceResultDto
        {
            StudentName = student.FullName,
            StudentCode = student.StudentCode ?? dto.StudentCode,
            Status = dto.Status,
            RecordedAt = attendance.Date,
            Method = "Manual"
        };
    }

    public async Task RecordAsync(int instructorId, RecordAttendanceDto dto)
    {
        var session = await Sessions.GetByIdAsync(dto.SessionId);
        if (session is null)
            throw new SessionNotFoundException("Session not found.");

        var classEntity = await Classes.GetByIdAsync(session.ClassId);
        if (classEntity?.InstructorId != instructorId)
            throw new InvalidOperationException("Not authorized.");

        var allStudents = await Students.GetAllAsync();

        foreach (var record in dto.Records)
        {
            var student = allStudents.FirstOrDefault(s => s.StudentCode == record.StudentCode);
            if (student is null) continue;

            var alreadyRecorded = await Attendances.AnyAsync(
                a => a.StudentId == student.UserId
                  && a.SessionId == dto.SessionId);

            if (alreadyRecorded) continue;

            Attendances.Add(new Attendance
            {
                SessionId = dto.SessionId,
                StudentId = student.UserId,
                Status = record.Status,
                Date = DateTime.UtcNow
            });
        }

        await _unitOfWork.SaveChangesAsync();

        foreach (var record in dto.Records)
        {
            var student = allStudents.FirstOrDefault(s => s.StudentCode == record.StudentCode);
            if (student is null) continue;

            await CheckAndNotifyThresholdAsync(
                student.UserId,
                classEntity.CourseId,
                classEntity.GroupCode ?? "");
        }
    }

    public async Task<IEnumerable<SessionDto>> GetByStudentAndCourseAsync(
        int studentId, int courseId)
    {
        var student = await Students.GetByIdAsync(studentId);
        if (student is null)
            throw new StudentNotFoundException(studentId);

        var course = await Courses.GetByIdAsync(courseId);
        if (course is null)
            throw new CourseNotFoundException(courseId);

        var classes = await Classes.GetAllAsync();
        var classIds = classes
            .Where(c => c.CourseId == courseId)
            .Select(c => c.ClassId)
            .ToHashSet();

        var allSessions = await Sessions.GetAllAsync();
        var sessions = allSessions
            .Where(s => classIds.Contains(s.ClassId))
            .ToList();

        return sessions.Select(s => new SessionDto
        {
            SessionId = s.SessionId,
            Date = s.Date,
            StartTime = s.StartTime?.ToString("hh:mm tt"),
            EndTime = s.EndTime?.ToString("hh:mm tt"),
            Topic = s.Topic,
            ClassId = s.ClassId,
            ClassName = s.Class?.GroupCode,
            SessionType = s.SessionType,
            TotalStudents = s.Attendances?.Count ?? 0,
            PresentCount = s.Attendances?
                .Count(a => a.StudentId == studentId
                         && (a.Status == AttendanceStatus.Present
                          || a.Status == AttendanceStatus.Late)) ?? 0
        });
    }

    public async Task<AttendanceReportDto> GenerateReportAsync(
        int classId, int instructorId)
    {
        var classEntity = await Classes.GetByIdAsync(classId);
        if (classEntity is null)
            throw new ClassNotFoundException("Class not found.");

        if (classEntity.InstructorId != instructorId)
            throw new InvalidOperationException("Not authorized.");

        var sessionSpec = new SessionSpec(classId, byClass: true);
        var sessions = (await Sessions.GetAllAsync(sessionSpec)).ToList();
        var totalSessions = sessions.Count;

        var allAttendances = sessions
            .SelectMany(s => s.Attendances)
            .ToList();

        var studentIds = allAttendances
            .Select(a => a.StudentId)
            .Distinct();

        var summaries = studentIds.Select(studentId =>
        {
            var sa = allAttendances
                .Where(a => a.StudentId == studentId)
                .ToList();

            var present = sa.Count(a => a.Status == AttendanceStatus.Present);
            var late = sa.Count(a => a.Status == AttendanceStatus.Late);
            var absent = sa.Count(a => a.Status == AttendanceStatus.Absent);
            var pct = totalSessions > 0
                ? Math.Round((decimal)(present + late) / totalSessions * 100, 1)
                : 0;

            return new StudentAttendanceSummary
            {
                StudentCode = sa.First().Student?.StudentCode ?? "",
                StudentName = sa.First().Student?.FullName,
                Present = present,
                Absent = absent,
                AttendancePercentage = pct,
                BelowThreshold = pct < AttendanceThreshold
            };
        }).ToList();

        var totalEntries = allAttendances.Count;

        var onTimePct = totalEntries > 0
            ? Math.Round((decimal)allAttendances
                .Count(a => a.Status == AttendanceStatus.Present)
                / totalEntries * 100, 1)
            : 0;

        var latePct = totalEntries > 0
            ? Math.Round((decimal)allAttendances
                .Count(a => a.Status == AttendanceStatus.Late)
                / totalEntries * 100, 1)
            : 0;

        return new AttendanceReportDto
        {
            ClassId = classId,
            ClassName = classEntity.GroupCode,
            TotalSessions = totalSessions,
            OnTimePercentage = onTimePct,
            NeedsImprovementPercentage = latePct,
            BelowThresholdCount = summaries.Count(s => s.BelowThreshold),
            Students = summaries
        };
    }

    public async Task<decimal> GetAttendancePercentageAsync(
        int studentId, int courseId)
    {
        var student = await Students.GetByIdAsync(studentId);
        if (student is null)
            throw new StudentNotFoundException(studentId);

        var course = await Courses.GetByIdAsync(courseId);
        if (course is null)
            throw new CourseNotFoundException(courseId);

        var classes = await Classes.GetAllAsync();
        var classIds = classes
            .Where(c => c.CourseId == courseId)
            .Select(c => c.ClassId)
            .ToHashSet();

        var allSessions = await Sessions.GetAllAsync();
        var sessions = allSessions
            .Where(s => classIds.Contains(s.ClassId))
            .ToList();

        var totalSessions = sessions.Count;
        if (totalSessions == 0) return 0;

        var sessionIds = sessions.Select(s => s.SessionId).ToHashSet();

        var attendanceRecords = sessions
            .SelectMany(s => s.Attendances ?? [])
            .Where(a => a.StudentId == studentId
                     && sessionIds.Contains(a.SessionId))
            .ToList();

        var present = attendanceRecords.Count(a =>
            a.Status == AttendanceStatus.Present ||
            a.Status == AttendanceStatus.Late);

        return Math.Round((decimal)present / totalSessions * 100, 1);
    }

    private async Task CheckAndNotifyThresholdAsync(
        int studentId, int courseId, string groupCode)
    {
        var percentage = await GetAttendancePercentageAsync(studentId, courseId);

        if (percentage < AttendanceThreshold)
        {
            await _notificationService.SendAsync(
                studentId,
                NotificationType.AttendanceWarning,
                $"Warning: Your attendance in {groupCode} dropped to {percentage}%. " +
                $"Minimum required is {AttendanceThreshold}%.");
        }
    }
}
