using IntelliCampus.Domain.Entities;
using IntelliCampus.Domain.Entities.Enums;
using IntelliCampus.Domain.Helpers;
using IntelliCampus.Domain.Interfaces;
using IntelliCampus.Service.Specifications;
using IntelliCampus.Service_Abstraction;
using IntelliCampus.Service.Exceptions;
using IntelliCampus.Shared.Dtos.Meeting;
namespace IntelliCampus.Service;

public class MeetingService(IUnitOfWork unitOfWork) : IMeetingService
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;

    private IGenericRepository<Meeting, int> Meetings
        => _unitOfWork.GetRepository<Meeting, int>();

    private IGenericRepository<Course, int> Courses
        => _unitOfWork.GetRepository<Course, int>();

    public async Task<IEnumerable<MeetingDto>> GetByCourseIdAsync(int courseId)
    {
        var course = await Courses.GetByIdAsync(courseId);
        if (course is null)
            throw new CourseNotFoundException(courseId);

        var meetings = await Meetings.GetAllAsync(new MeetingByCourseSpec(courseId), asNoTracking: true);
        return meetings.Select(MapToDto).ToList();
    }

    public async Task<MeetingDto?> GetByIdAsync(int meetingId)
    {
        var meeting = await Meetings.GetByIdAsync(meetingId);
        return meeting is null ? null : MapToDto(meeting);
    }

    private async Task EnsureCourseActiveAsync(int courseId)
    {
        var course = await Courses.GetByIdAsync(courseId);
        if (course is null) throw new CourseNotFoundException(courseId);
        if (course.Status != CourseStatus.Active)
            throw new InvalidOperationException("This course is finalized and read-only.");
    }

    public async Task<MeetingDto> CreateAsync(CreateMeetingDto dto, int instructorId)
    {
        await EnsureCourseActiveAsync(dto.CourseId);

        var roomName = $"Course-{dto.CourseId}-{Guid.NewGuid().ToString()[..8]}";

        var meeting = new Meeting
        {
            Title = dto.Title,
            DateTime = EgyptTime.Now,
            RoomName = roomName,
            CourseId = dto.CourseId,
            InstructorId = instructorId,
            IsActive = true
        };

        Meetings.Add(meeting);
        await _unitOfWork.SaveChangesAsync();

        return MapToDto(meeting);
    }

    public async Task<bool> EndMeetingAsync(int meetingId, int instructorId)
    {
        var meeting = await Meetings.GetByIdAsync(meetingId);
        if (meeting is null) throw new MeetingNotFoundException($"Meeting with ID {meetingId} not found.");

        await EnsureCourseActiveAsync(meeting.CourseId);

        var sql = "UPDATE [Meetings] SET [IsActive] = 0 WHERE [MeetingId] = {0} AND [InstructorId] = {1}";
        await _unitOfWork.ExecuteSqlAsync(sql, meetingId, instructorId);

        return true;
    }

    public async Task<bool> DeleteAsync(int meetingId)
    {
        var meeting = await Meetings.GetByIdAsync(meetingId);
        if (meeting is null) throw new MeetingNotFoundException($"Meeting with ID {meetingId} not found.");

        await EnsureCourseActiveAsync(meeting.CourseId);

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
        InstructorId = meeting.InstructorId,
        IsActive = meeting.IsActive
    };
}
