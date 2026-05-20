using IntelliCampus.Domain.Entities;
using IntelliCampus.Domain.Entities.Enums;
using IntelliCampus.Domain.Interfaces;
using IntelliCampus.Service.Specifications;
using IntelliCampus.Service_Abstraction;
using IntelliCampus.shared.Dtos.Attendance;

namespace IntelliCampus.Service;

public class AttendanceService : IAttendanceService
{
    private readonly IUnitOfWork _unitOfWork;

    private const decimal AttendanceThreshold = 75m;

    public AttendanceService(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    private IGenericRepository<Attendance, int> Attendances => _unitOfWork.GetRepository<Attendance, int>();
    private IGenericRepository<Session, int> Sessions => _unitOfWork.GetRepository<Session, int>();
    private IGenericRepository<Class, int> Classes => _unitOfWork.GetRepository<Class, int>();
    private IGenericRepository<Student, int> Students => _unitOfWork.GetRepository<Student, int>();

    public async Task RecordAsync(int instructorId, RecordAttendanceDto dto)
    {
        var session = await Sessions.GetByIdAsync(dto.SessionId);
        if (session is null)
            throw new InvalidOperationException("Session not found.");

        var classEntity = await Classes.GetByIdAsync(session.ClassId);
        if (classEntity?.InstructorId != instructorId)
            throw new InvalidOperationException("Not authorized.");

        var date = DateTime.UtcNow;

        foreach (var record in dto.Records)
        {
            var attendance = new Attendance
            {
                SessionId = dto.SessionId,
                StudentId = record.StudentId,
                Status = record.Status,
                Date = date
            };
            Attendances.Add(attendance);
        }

        await _unitOfWork.SaveChangesAsync();
    }

    public async Task<IEnumerable<SessionDto>> GetByStudentAndCourseAsync(int studentId, int courseId)
    {
        var classes = await Classes.GetAllAsync();
        var classIds = classes.Where(c => c.CourseId == courseId).Select(c => c.ClassId).ToHashSet();

        var allSessions = await Sessions.GetAllAsync();
        var sessions = allSessions.Where(s => classIds.Contains(s.ClassId)).ToList();

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
            PresentCount = s.Attendances?.Count(a => a.StudentId == studentId && (a.Status == AttendanceStatus.Present || a.Status == AttendanceStatus.Late)) ?? 0
        });
    }

    public async Task<AttendanceReportDto> GenerateReportAsync(int classId, int instructorId)
    {
        var classEntity = await Classes.GetByIdAsync(classId);
        if (classEntity is null)
            throw new InvalidOperationException("Class not found.");
        if (classEntity.InstructorId != instructorId)
            throw new InvalidOperationException("Not authorized.");

        var sessionSpec = new SessionSpec(classId, byClass: true);
        var sessions = (await Sessions.GetAllAsync(sessionSpec)).ToList();
        var totalSessions = sessions.Count;

        var allAttendances = sessions.SelectMany(s => s.Attendances).ToList();
        var studentIds = allAttendances.Select(a => a.StudentId).Distinct();

        var summaries = studentIds.Select(studentId =>
        {
            var sa = allAttendances.Where(a => a.StudentId == studentId).ToList();
            var present = sa.Count(a => a.Status == AttendanceStatus.Present);
            var late = sa.Count(a => a.Status == AttendanceStatus.Late);
            var absent = sa.Count(a => a.Status == AttendanceStatus.Absent);
            var pct = totalSessions > 0 ? Math.Round((decimal)(present + late) / totalSessions * 100, 1) : 0;

            return new StudentAttendanceSummary
            {
                StudentId = studentId,
                StudentName = sa.First().Student?.FullName,
                Present = present,
                Late = late,
                Absent = absent,
                AttendancePercentage = pct,
                BelowThreshold = pct < AttendanceThreshold
            };
        }).ToList();

        var totalEntries = allAttendances.Count;
        var onTimePct = totalEntries > 0 ? Math.Round((decimal)allAttendances.Count(a => a.Status == AttendanceStatus.Present) / totalEntries * 100, 1) : 0;
        var latePct = totalEntries > 0 ? Math.Round((decimal)allAttendances.Count(a => a.Status == AttendanceStatus.Late) / totalEntries * 100, 1) : 0;

        return new AttendanceReportDto
        {
            ClassId = classId,
            ClassName = classEntity.GroupCode,
            TotalSessions = totalSessions,
            OnTimePercentage = onTimePct,
            NeedsImprovementPercentage = latePct,
            Students = summaries
        };
    }

    public async Task<decimal> GetAttendancePercentageAsync(int studentId, int courseId)
    {
        var classes = await Classes.GetAllAsync();
        var classIds = classes.Where(c => c.CourseId == courseId).Select(c => c.ClassId).ToHashSet();

        var allSessions = await Sessions.GetAllAsync();
        var sessions = allSessions.Where(s => classIds.Contains(s.ClassId)).ToList();
        var totalSessions = sessions.Count;

        if (totalSessions == 0) return 0;

        var sessionIds = sessions.Select(s => s.SessionId).ToHashSet();
        var attendanceRecords = allSessions
            .SelectMany(s => s.Attendances ?? [])
            .Where(a => a.StudentId == studentId && sessionIds.Contains(a.SessionId))
            .ToList();

        var present = attendanceRecords.Count(a => a.Status == AttendanceStatus.Present || a.Status == AttendanceStatus.Late);
        return Math.Round((decimal)present / totalSessions * 100, 1);
    }
}
