using IntelliCampus.shared.Dtos.Attendance;

namespace IntelliCampus.Service_Abstraction;

public interface ISessionService
{
    Task<SessionDto> GetByIdAsync(int sessionId);

    Task<IEnumerable<SessionDto>> GetByClassIdAsync(int classId);

    Task<SessionDto> CreateAsync(int instructorId, CreateSessionDto dto);

    Task DeleteAsync(int sessionId, int instructorId);
}
