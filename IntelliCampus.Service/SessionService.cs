using IntelliCampus.Domain.Entities;
using IntelliCampus.Domain.Entities.Enums;
using IntelliCampus.Domain.Interfaces;
using IntelliCampus.Service.Exceptions;
using IntelliCampus.Service.Specifications;
using IntelliCampus.Service_Abstraction;
using IntelliCampus.shared.Dtos.Attendance;
using Microsoft.Extensions.Logging;

namespace IntelliCampus.Service;

public class SessionService : ISessionService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<SessionService> _logger;

    public SessionService(IUnitOfWork unitOfWork, ILogger<SessionService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    private IGenericRepository<Session, int> Sessions => _unitOfWork.GetRepository<Session, int>();
    private IGenericRepository<Class, int> Classes => _unitOfWork.GetRepository<Class, int>();
    private IGenericRepository<Course, int> Courses => _unitOfWork.GetRepository<Course, int>();

    private async Task EnsureCourseActiveAsync(int courseId)
    {
        var course = await Courses.GetByIdAsync(courseId);
        if (course is null) throw new KeyNotFoundException("Course not found.");
        if (course.Status != CourseStatus.Active)
            throw new InvalidOperationException("This course is finalized and read-only.");
    }

    public async Task<SessionDto> GetByIdAsync(int sessionId)
    {
        var spec = new SessionSpec(sessionId);
        var session = await Sessions.GetByIdAsync(spec) ?? throw new SessionNotFoundException(sessionId);
        return MapToDto(session);
    }

    public async Task<IEnumerable<SessionDto>> GetByClassIdAsync(int classId)
    {
        var classEntity = await Classes.GetByIdAsync(classId);
        if (classEntity is null)
            throw new ClassNotFoundException(classId);

        var spec = new SessionSpec(classId, byClass: true);
        var sessions = await Sessions.GetAllAsync(spec, asNoTracking: true);
        return sessions.Select(MapToDto);
    }

    public async Task<SessionDto> CreateAsync(int instructorId, CreateSessionDto dto)
    {
        try
        {
            var classEntity = await Classes.GetByIdAsync(dto.ClassId);
            if (classEntity is null)
                throw new ClassNotFoundException(dto.ClassId);
            await EnsureCourseActiveAsync(classEntity.CourseId);
            if (classEntity.InstructorId != instructorId)
                throw new InvalidOperationException("Not authorized.");

            var session = new Session
            {
                ClassId = dto.ClassId,
                Date = dto.Date,
                StartTime = dto.StartTime,
                EndTime = dto.EndTime,
                Topic = dto.Topic,
                SessionType = dto.SessionType
            };

            Sessions.Add(session);
            await _unitOfWork.SaveChangesAsync();

            var spec = new SessionSpec(session.SessionId);
            var result = await Sessions.GetByIdAsync(spec);
            return MapToDto(result!);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create session for class {ClassId} by instructor {InstructorId}", dto.ClassId, instructorId);
            throw;
        }
    }

    public async Task DeleteAsync(int sessionId, int instructorId)
    {
        var session = await Sessions.GetByIdAsync(sessionId) ?? throw new SessionNotFoundException(sessionId);

        var classEntity = await Classes.GetByIdAsync(session.ClassId);
        if (classEntity?.InstructorId != instructorId)
            throw new InvalidOperationException("Not authorized.");

        Sessions.Delete(session);
        await _unitOfWork.SaveChangesAsync();
    }

    private static SessionDto MapToDto(Session s) => new()
    {
        SessionId = s.SessionId,
        Date = s.Date,
        StartTime = s.StartTime?.ToString("hh:mm tt"),
        EndTime = s.EndTime?.ToString("hh:mm tt"),
        Topic = s.Topic,
        ClassId = s.ClassId,
        ClassName = s.Class?.GroupCode,
        SessionType = s.SessionType,
        TotalStudents = s.Class?.StudentCourses?.Count ?? s.Attendances?.Count ?? 0,
        PresentCount = s.Attendances?.Count(a => a.Status == AttendanceStatus.Present
                                              || a.Status == AttendanceStatus.Excused) ?? 0
    };
}
