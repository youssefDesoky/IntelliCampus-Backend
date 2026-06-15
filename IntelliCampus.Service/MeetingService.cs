using IntelliCampus.Domain.Entities;
using IntelliCampus.Domain.Interfaces;
using IntelliCampus.Service_Abstraction;
using IntelliCampus.Shared.Dtos.Meeting;

namespace IntelliCampus.Service;

public class MeetingService(IUnitOfWork unitOfWork) : IMeetingService
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;

    private IGenericRepository<Meeting, int> Meetings
        => _unitOfWork.GetRepository<Meeting, int>();

    public async Task<IEnumerable<MeetingDto>> GetByCourseIdAsync(int courseId)
    {
        var all = await Meetings.GetAllAsync();
        return all
            .Where(m => m.CourseId == courseId)
            .OrderByDescending(m => m.DateTime)
            .Select(MapToDto);
    }

    public async Task<MeetingDto> CreateAsync(CreateMeetingDto dto, int instructorId)
    {
        var roomName = $"Course-{dto.CourseId}-{Guid.NewGuid().ToString()[..8]}";

        var meeting = new Meeting
        {
            Title = dto.Title,
            DateTime = dto.DateTime,
            RoomName = roomName,
            CourseId = dto.CourseId,
            InstructorId = instructorId
        };

        Meetings.Add(meeting);
        await _unitOfWork.SaveChangesAsync();

        return MapToDto(meeting);
    }

    public async Task<bool> DeleteAsync(int meetingId)
    {
        var meeting = await Meetings.GetByIdAsync(meetingId);
        if (meeting is null) return false;

        Meetings.Delete(meeting);
        await _unitOfWork.SaveChangesAsync();
        return true;
    }

    private static MeetingDto MapToDto(Meeting meeting) => new()
    {
        MeetingId = meeting.MeetingId,
        Title = meeting.Title,
        DateTime = meeting.DateTime,
        RoomName = meeting.RoomName,
        CourseId = meeting.CourseId,
        InstructorId = meeting.InstructorId
    };
}
