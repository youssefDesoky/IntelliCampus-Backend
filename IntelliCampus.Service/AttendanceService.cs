using System.Text;
using System.Text.Json;
using IntelliCampus.Domain.Entities;
using IntelliCampus.Domain.Entities.Enums;
using IntelliCampus.Domain.Interfaces;
using IntelliCampus.Service.Exceptions;
using IntelliCampus.Service.Specifications;
using IntelliCampus.Service_Abstraction;
using IntelliCampus.shared.Dtos.Attendance;
using IntelliCampus.shared.Pagination;
using IntelliCampus.Shared.Params;

namespace IntelliCampus.Service;

public class AttendanceService : IAttendanceService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly INotificationService _notificationService;

    private const decimal AttendanceThreshold = 75m;
    private const int QrValidSeconds = 45;

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

    private IGenericRepository<StudentCourse, (int, int)> StudentCourses
        => _unitOfWork.GetRepository<StudentCourse, (int, int)>();

    private IGenericRepository<Course, int> Courses
        => _unitOfWork.GetRepository<Course, int>();

    private async Task EnsureCourseActiveAsync(int courseId)
    {
        var course = await Courses.GetByIdAsync(courseId);
        if (course is null) throw new KeyNotFoundException("Course not found.");
        if (course.Status != CourseStatus.Active)
            throw new InvalidOperationException("This course is finalized and read-only.");
    }

    private IGenericRepository<QrToken, int> QrTokens
        => _unitOfWork.GetRepository<QrToken, int>();

    public async Task<QrTokenDto> GenerateQrAsync(int studentId)
    {
        var student = await Students.GetByIdAsync(studentId);
        if (student is null)
            throw new StudentNotFoundException(studentId);

        var now = EgyptTime.Now;

        var rawToken = Convert.ToBase64String(
            Encoding.UTF8.GetBytes(
                $"{studentId}:{now.Ticks}:{Guid.NewGuid()}"));

        var payload = new QrPayload
        {
            UserId = studentId,
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

        await EnsureCourseActiveAsync(classEntity.CourseId);

        QrPayload payload;
        try
        {
            var json = Encoding.UTF8.GetString(
                Convert.FromBase64String(dto.QrPayload));
            payload = JsonSerializer.Deserialize<QrPayload>(json)
                ?? throw new InvalidOperationException("Null payload");
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

        var student = await Students.GetByIdAsync(payload.UserId);
        var studentName = student?.User?.FullName ?? "Student";
        var studentCode = student?.StudentCode ?? payload.UserId.ToString();

        if (student is not null)
        {
            var isEnrolled = await StudentCourses.AnyAsync(
                sc => sc.StudentId == student.UserId
                   && sc.CourseId == classEntity.CourseId
                   && sc.Status == StudentCourseStatus.InProgress);
            if (!isEnrolled)
                throw new InvalidOperationException(
                    $"{studentName} is not enrolled in this course.");
        }

        var alreadyRecorded = await Attendances.AnyAsync(
            a => a.StudentId == payload.UserId
              && a.SessionId == dto.SessionId);

        if (alreadyRecorded)
            throw new InvalidOperationException(
                $"{studentName} is already recorded for this session.");

        qrToken.ExpiresAt = EgyptTime.Now.AddSeconds(-1);
        QrTokens.Update(qrToken);

        var attendance = new Attendance
        {
            StudentId = payload.UserId,
            SessionId = dto.SessionId,
            Status = dto.Status,
            Date = EgyptTime.Now
        };

        Attendances.Add(attendance);
        await _unitOfWork.SaveChangesAsync();

        await CheckAndNotifyThresholdAsync(
            payload.UserId,
            classEntity.CourseId,
            classEntity.GroupCode ?? "");

        return new AttendanceResultDto
        {
            StudentName = studentName,
            StudentCode = studentCode,
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

        await EnsureCourseActiveAsync(classEntity.CourseId);

        var student = await Students.GetByIdAsync(new StudentSpec(dto.StudentCode, byCode: true));
        if (student is null)
            throw new InvalidOperationException($"Student with code '{dto.StudentCode}' not found.");

        var isEnrolled = await StudentCourses.AnyAsync(
            sc => sc.StudentId == student.UserId
               && sc.CourseId == classEntity.CourseId
               && sc.Status == StudentCourseStatus.InProgress);

        if (!isEnrolled)
            throw new InvalidOperationException(
                $"{student.User.FullName} is not enrolled in this course.");

        var alreadyRecorded = await Attendances.AnyAsync(
            a => a.StudentId == student.UserId
              && a.SessionId == dto.SessionId);

        if (alreadyRecorded)
            throw new InvalidOperationException(
                $"{student.User.FullName} is already recorded for this session.");

        var attendance = new Attendance
        {
            StudentId = student.UserId,
            SessionId = dto.SessionId,
            Status = dto.Status,
            Date = EgyptTime.Now
        };

        Attendances.Add(attendance);
        await _unitOfWork.SaveChangesAsync();

        await CheckAndNotifyThresholdAsync(
            student.UserId,
            classEntity.CourseId,
            classEntity.GroupCode ?? "");

        return new AttendanceResultDto
        {
            StudentName = student.User.FullName,
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

        await EnsureCourseActiveAsync(classEntity.CourseId);

        var studentCodes = dto.Records.Select(r => r.StudentCode).Distinct().ToList();
        var students = (await Students.GetAllAsync(new StudentSpec(studentCodes, byCodes: true), asNoTracking: true))
            .ToDictionary(s => s.StudentCode ?? "", s => s);
        var now = EgyptTime.Now;

        // Only allow attendance for students enrolled in the course with InProgress status
        var enrolledIds = students.Count > 0
            ? (await StudentCourses.GetAllAsync(
                new StudentCourseIdsSpec(students.Values.Select(s => s.UserId).ToList(), StudentCourseStatus.InProgress),
                asNoTracking: true))
                .Where(sc => sc.CourseId == classEntity.CourseId)
                .Select(sc => sc.StudentId)
                .ToHashSet()
            : new HashSet<int>();

        // Batch check which students already have attendance recorded for this session
        var studentIds = students.Values.Select(s => s.UserId).ToHashSet();
        var existingAttendance = studentIds.Count > 0
            ? await Attendances.GetAllAsync(new AttendanceSpec(dto.SessionId, studentIds), asNoTracking: true)
            : new List<Attendance>();
        var alreadyRecordedIds = existingAttendance.Select(a => a.StudentId).ToHashSet();

        foreach (var record in dto.Records)
        {
            if (!students.TryGetValue(record.StudentCode, out var student)) continue;
            if (alreadyRecordedIds.Contains(student.UserId)) continue;
            if (!enrolledIds.Contains(student.UserId)) continue;

            Attendances.Add(new Attendance
            {
                SessionId = dto.SessionId,
                StudentId = student.UserId,
                Status = record.Status,
                Date = now
            });
        }

        await _unitOfWork.SaveChangesAsync();

        // Batch notify about threshold — check all students after save (single DB pass)
        var classesForThreshold = classEntity.CourseId > 0
            ? await Classes.GetAllAsync(new ClassSpec(classEntity.CourseId, byCourse: true), asNoTracking: true)
            : [];
        var classIdsForThreshold = classesForThreshold.Select(c => c.ClassId).ToHashSet();
        var sessionsForThreshold = classIdsForThreshold.Count > 0
            ? (await Sessions.GetAllAsync(new SessionSpec(classIdsForThreshold), asNoTracking: true)).ToList()
            : [];

        foreach (var student in students.Values)
        {
            var totalSessions = sessionsForThreshold.Count;
            var present = totalSessions > 0
                ? sessionsForThreshold
                    .SelectMany(s => s.Attendances ?? [])
                    .Count(a => a.StudentId == student.UserId && a.Status == AttendanceStatus.Present)
                : 0;
            var percentage = totalSessions > 0
                ? Math.Round((decimal)present / totalSessions * 100, 1)
                : 0;

            if (percentage < AttendanceThreshold)
            {
                await _notificationService.SendAsync(
                    student.UserId,
                    NotificationType.AttendanceWarning,
                    $"Warning: Your attendance in {classEntity.GroupCode} dropped to {percentage}%. " +
                    $"Minimum required is {AttendanceThreshold}%.",
                    clickUrl: $"/courses/{classEntity.CourseId}/attendance");
            }
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

        var classIds = (await Classes.GetAllAsync(new ClassSpec(courseId, byCourse: true), asNoTracking: true))
            .Select(c => c.ClassId)
            .ToHashSet();

        var sessions = classIds.Count > 0
            ? (await Sessions.GetAllAsync(new SessionSpec(classIds), asNoTracking: true)).ToList()
            : new List<Session>();

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
                          || a.Status == AttendanceStatus.Excused)) ?? 0
        });
    }

    public async Task<PaginatedResult<SessionDto>> GetByStudentAndCourseAsync(
        int studentId, int courseId, SessionQueryParams queryParams)
    {
        var student = await Students.GetByIdAsync(studentId);
        if (student is null)
            throw new StudentNotFoundException(studentId);

        var course = await Courses.GetByIdAsync(courseId);
        if (course is null)
            throw new CourseNotFoundException(courseId);

        var classIds = (await Classes.GetAllAsync(new ClassSpec(courseId, byCourse: true), asNoTracking: true))
            .Select(c => c.ClassId)
            .ToHashSet();

        var spec = new SessionSpec(classIds, queryParams);
        var sessions = await Sessions.GetAllAsync(spec, asNoTracking: true);
        var dataToReturn = sessions.Select(s => new SessionDto
        {
            SessionId = s.SessionId,
            Date = s.Date,
            StartTime = s.StartTime?.ToString("hh:mm tt"),
            EndTime = s.EndTime?.ToString("hh:mm tt"),
            Topic = s.Topic,
            ClassId = s.ClassId,
            ClassName = s.Class?.GroupCode,
            SessionType = s.SessionType,
            TotalStudents = s.Class?.StudentCourses?.Count(sc => sc.Status == StudentCourseStatus.InProgress) ?? s.Attendances?.Count ?? 0,
            PresentCount = s.Attendances?
                .Count(a => a.StudentId == studentId
                         && (a.Status == AttendanceStatus.Present
                          || a.Status == AttendanceStatus.Excused)) ?? 0
        }).ToList();
        var countSpec = new SessionCountSpec(classIds);
        var totalCount = await Sessions.CountAsync(countSpec);
        return new PaginatedResult<SessionDto>(queryParams.PageIndex, dataToReturn.Count, totalCount, dataToReturn);
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
        var sessions = (await Sessions.GetAllAsync(sessionSpec, asNoTracking: true)).ToList();
        var totalSessions = sessions.Count;

        var allAttendances = sessions
            .SelectMany(s => s.Attendances)
            .ToList();

        var enrolledStudentIds = (await StudentCourses.GetAllAsync(new StudentCourseIdsSpec(classEntity.CourseId, byCourse: true, StudentCourseStatus.InProgress), asNoTracking: true))
            .Select(sc => sc.StudentId)
            .ToHashSet();

        var studentIds = allAttendances
            .Select(a => a.StudentId)
            .Where(id => enrolledStudentIds.Contains(id))
            .Concat(enrolledStudentIds)
            .Distinct();

        var summaries = studentIds.Select(studentId =>
        {
            var sa = allAttendances
                .Where(a => a.StudentId == studentId)
                .ToList();

            var present = sa.Count(a => a.Status == AttendanceStatus.Present);
            var absent = sa.Count(a => a.Status == AttendanceStatus.Absent);
            var pct = totalSessions > 0
                ? Math.Round((decimal)present / totalSessions * 100, 1)
                : 0;

            return new StudentAttendanceSummary
            {
                StudentCode = sa.FirstOrDefault()?.Student?.StudentCode ?? "",
                StudentName = sa.FirstOrDefault()?.Student?.User?.FullName,
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

        return new AttendanceReportDto
        {
            ClassId = classId,
            ClassName = classEntity.GroupCode,
            TotalSessions = totalSessions,
            OnTimePercentage = onTimePct,
            NeedsImprovementPercentage = 0,
            BelowThresholdCount = summaries.Count(s => s.BelowThreshold),
            Students = summaries
        };
    }

    public async Task<PaginatedResult<AttendanceReportDto>> GenerateReportAsync(int classId, int instructorId, SessionQueryParams queryParams)
    {
        var report = await GenerateReportAsync(classId, instructorId);
        var wrapped = new List<AttendanceReportDto> { report };
        return new PaginatedResult<AttendanceReportDto>(queryParams.PageIndex, wrapped.Count, wrapped.Count, wrapped);
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

        var classIds = (await Classes.GetAllAsync(new ClassSpec(courseId, byCourse: true), asNoTracking: true))
            .Select(c => c.ClassId)
            .ToHashSet();

        if (classIds.Count == 0) return 0;

        var sessions = await Sessions.GetAllAsync(new SessionSpec(classIds), asNoTracking: true);
        var sessionsList = sessions.ToList();
        var totalSessions = sessionsList.Count;
        if (totalSessions == 0) return 0;

        var present = sessionsList
            .SelectMany(s => s.Attendances ?? [])
            .Count(a => a.StudentId == studentId
                     && a.Status == AttendanceStatus.Present);

        return Math.Round((decimal)present / totalSessions * 100, 1);
    }

    public async Task<SessionAttendanceDto> GetSessionAttendanceAsync(int sessionId, int instructorId)
    {
        var session = await Sessions.GetByIdAsync(sessionId);
        if (session is null)
            throw new SessionNotFoundException("Session not found.");

        var classEntity = await Classes.GetByIdAsync(session.ClassId);
        if (classEntity?.InstructorId != instructorId)
            throw new InvalidOperationException("Not authorized.");

        var sessionAttendance = await Attendances.GetAllAsync(new AttendanceSpec(sessionId, bySession: true), asNoTracking: true);
        var attendanceByStudent = sessionAttendance.ToDictionary(a => a.StudentId);

        // Collect all student IDs: enrolled + anyone with attendance (manual recording bypasses enrollment)
        var enrolledStudentIds = (await StudentCourses.GetAllAsync(new StudentCourseIdsSpec(classEntity.CourseId, byCourse: true, StudentCourseStatus.InProgress), asNoTracking: true))
            .Select(sc => sc.StudentId)
            .ToHashSet();

        var allStudentIds = enrolledStudentIds
            .Concat(attendanceByStudent.Keys)
            .Distinct()
            .ToList();

        var allStudents = (await Students.GetAllAsync(new StudentSpec(allStudentIds), asNoTracking: true))
            .ToDictionary(s => s.UserId);

        var students = allStudentIds
            .Select(id =>
            {
                allStudents.TryGetValue(id, out var student);
                var record = attendanceByStudent.GetValueOrDefault(id);
                return new SessionAttendanceStudentDto
                {
                    StudentId = id,
                    StudentCode = student?.StudentCode ?? "",
                    FullName = student?.User?.FullName ?? "Unknown",
                    Status = record is not null ? record.Status : AttendanceStatus.NotRecorded,
                    CheckInTime = record?.Date
                };
            })
            .OrderBy(s => s.FullName)
            .ToList();

        return new SessionAttendanceDto
        {
            SessionId = session.SessionId,
            Topic = session.Topic,
            Date = session.Date,
            StartTime = session.StartTime?.ToString("hh:mm tt"),
            EndTime = session.EndTime?.ToString("hh:mm tt"),
            SessionType = session.SessionType.ToString(),
            ClassName = classEntity.GroupCode,
            TotalStudents = students.Count,
            PresentCount = students.Count(s => s.Status == AttendanceStatus.Present),
            Students = students
        };
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
                $"Minimum required is {AttendanceThreshold}%.",
                clickUrl: $"/courses/{courseId}/attendance");
        }
    }
}
