using IntelliCampus.Domain.Entities;
using IntelliCampus.Domain.Entities.Enums;
using IntelliCampus.Domain.Interfaces;
using IntelliCampus.Service.Specifications;
using IntelliCampus.Service_Abstraction;
using IntelliCampus.Shared.Dtos.Schedule;

namespace IntelliCampus.Service;

public class ScheduleService : IScheduleService
{
    private readonly IUnitOfWork _unitOfWork;

    public ScheduleService(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    private IGenericRepository<Schedule, int> Schedules
        => _unitOfWork.GetRepository<Schedule, int>();

    private IGenericRepository<Class, int> Classes
        => _unitOfWork.GetRepository<Class, int>();

    public async Task<ScheduleDto?> GetByIdAsync(int scheduleId)
    {
        var spec = new ScheduleSpec(scheduleId, byId: true);
        var schedule = await Schedules.GetByIdAsync(spec);
        return schedule is null ? null : MapToDto(schedule);
    }

    public async Task<IEnumerable<ScheduleDto>> GetByStudentIdAsync(int studentId)
    {
        var spec = new ScheduleSpec(studentId);
        var schedules = await Schedules.GetAllAsync(spec);
        return schedules.Select(MapToDto);
    }

    public async Task<IEnumerable<ScheduleDto>> GetByStudentIdAndTypeAsync(int studentId, ScheduleType type)
    {
        var spec = new ScheduleSpec(studentId, type);
        var schedules = await Schedules.GetAllAsync(spec);
        return schedules.Select(MapToDto);
    }

    public async Task<IEnumerable<ScheduleDto>> GetByStudentIdAndTypesAsync(int studentId, IReadOnlyCollection<ScheduleType> types)
    {
        if (types is null || types.Count == 0)
            return await GetByStudentIdAsync(studentId);

        // Reuse existing spec and apply an IN filter in-memory.
        // If you want this server-side, add a dedicated Specification that translates to SQL IN.
        var spec = new ScheduleSpec(studentId);
        var schedules = await Schedules.GetAllAsync(spec);

        var typeSet = types is HashSet<ScheduleType> hs ? hs : types.ToHashSet();
        return schedules
            .Where(s => typeSet.Contains(s.ScheduleType))
            .Select(MapToDto);
    }

    public async Task SyncFromCourseRegistrationAsync(int studentId, int classId)
    {
        var cls = await Classes.GetByIdAsync(new ClassByIdSpec(classId));
        if (cls is null)
            throw new InvalidOperationException("Class not found.");

        if (cls.StartTime is null || cls.EndTime is null)
            throw new InvalidOperationException("Class schedule is not fully defined (StartTime/EndTime).");

        var schedule = new Schedule
        {
            Title = cls.Course.CourseName,
            Day = cls.Day?.ToString() ?? string.Empty,
            StartTime = cls.StartTime.Value,
            EndTime = cls.EndTime.Value,
            Location = cls.Room,
            ScheduleType = cls.ClassType switch
            {
                ClassType.Lecture => ScheduleType.Lecture,
                ClassType.Section => ScheduleType.Section,
                ClassType.Lab => ScheduleType.Activity,
                _ => ScheduleType.Lecture
            },
            InstructorName = cls.Instructor?.FullName,
            CourseId = cls.CourseId,
            StudentId = studentId,
            ClassId = cls.ClassId,
            // Schedule.Date is part of existing model; keep a stable value (min) since module is weekly based.
            Date = DateTime.MinValue
        };

        Schedules.Add(schedule);
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task RemoveByStudentAndCourseAsync(int studentId, int courseId)
    {
        var schedules = await Schedules.GetAllAsync(new ScheduleByStudentAndCourseSpec(studentId, courseId));
        foreach (var s in schedules)
            Schedules.Delete(s);

        await _unitOfWork.SaveChangesAsync();
    }

    public async Task SyncFromClassUpdateAsync(int classId)
    {
        var cls = await Classes.GetByIdAsync(new ClassByIdSpec(classId));
        if (cls is null)
            throw new InvalidOperationException("Class not found.");

        if (cls.StartTime is null || cls.EndTime is null)
            throw new InvalidOperationException("Class schedule is not fully defined (StartTime/EndTime).");

        var schedules = await Schedules.GetAllAsync(new ScheduleByClassIdSpec(classId));
        foreach (var s in schedules)
        {
            s.Day = cls.Day?.ToString() ?? string.Empty;
            s.StartTime = cls.StartTime.Value;
            s.EndTime = cls.EndTime.Value;
            s.Location = cls.Room;
            s.InstructorName = cls.Instructor?.FullName;
            Schedules.Update(s);
        }

        await _unitOfWork.SaveChangesAsync();
    }

    private static ScheduleDto MapToDto(Schedule s) => new()
    {
        ScheduleId = s.ScheduleId,
        Title = s.Title,
        Day = s.Day,
        Date = s.Date,
        StartTime = s.StartTime,
        EndTime = s.EndTime,
        Location = s.Location,
        Type = s.ScheduleType,
        InstructorName = s.InstructorName,
        CourseId = s.CourseId,
        CourseName = s.Course?.CourseName,
        StudentId = s.StudentId
    };
}
