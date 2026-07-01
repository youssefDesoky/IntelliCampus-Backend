using IntelliCampus.Shared.Dtos.Meeting;

namespace IntelliCampus.Service_Abstraction;

public interface IMeetingService
{
    Task<IEnumerable<MeetingDto>> GetByCourseIdAsync(int courseId);
    Task<MeetingDto?> GetByIdAsync(int meetingId);
    Task<MeetingDto> CreateAsync(CreateMeetingDto dto, int instructorId);
    Task<bool> EndMeetingAsync(int meetingId, int instructorId);
    Task<bool> DeleteAsync(int meetingId);
}
