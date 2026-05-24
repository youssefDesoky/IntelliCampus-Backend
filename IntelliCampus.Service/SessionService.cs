using IntelliCampus.Domain.Entities;
using IntelliCampus.Domain.Entities.Enums;
using IntelliCampus.Domain.Interfaces;
using IntelliCampus.Service.Specifications;
using IntelliCampus.Service_Abstraction;
using IntelliCampus.shared.Dtos.Attendance;

namespace IntelliCampus.Service;

public class SessionService : ISessionService
{
    private readonly IUnitOfWork _unitOfWork;

    public SessionService(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    private IGenericRepository<Session, int> Sessions => _unitOfWork.GetRepository<Session, int>();
    private IGenericRepository<Class, int> Classes => _unitOfWork.GetRepository<Class, int>();

    public async Task<SessionDto?> GetByIdAsync(int sessionId)
    {
        var spec = new SessionSpec(sessionId);
        var session = await Sessions.GetByIdAsync(spec);
        return session is null ? null : MapToDto(session);
    }

    public async Task<IEnumerable<SessionDto>> GetByClassIdAsync(int classId)
    {
        var spec = new SessionSpec(classId, byClass: true);
        var sessions = await Sessions.GetAllAsync(spec);
        return sessions.Select(MapToDto);
    }

    public async Task<SessionDto> CreateAsync(int instructorId, CreateSessionDto dto)
    {
        var classEntity = await Classes.GetByIdAsync(dto.ClassId);
        if (classEntity is null)
            throw new InvalidOperationException("Class not found.");
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

    public async Task<bool> DeleteAsync(int sessionId, int instructorId)
    {
        var session = await Sessions.GetByIdAsync(sessionId);
        if (session is null)
            return false;

        var classEntity = await Classes.GetByIdAsync(session.ClassId);
        if (classEntity?.InstructorId != instructorId)
            throw new InvalidOperationException("Not authorized.");

        Sessions.Delete(session);
        await _unitOfWork.SaveChangesAsync();
        return true;
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
        TotalStudents = s.Attendances?.Count ?? 0,
        PresentCount = s.Attendances?.Count(a => a.Status == AttendanceStatus.Present || a.Status == AttendanceStatus.Late) ?? 0
    };
}
