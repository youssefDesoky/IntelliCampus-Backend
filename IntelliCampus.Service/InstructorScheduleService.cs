using IntelliCampus.Domain.Entities;
using IntelliCampus.Domain.Entities.Enums;
using IntelliCampus.Domain.Interfaces;
using IntelliCampus.Service.Exceptions;
using IntelliCampus.Service.Specifications;
using IntelliCampus.Service_Abstraction;
using IntelliCampus.Shared.Dtos.Export;
using IntelliCampus.Shared.Dtos.Schedule;
using IntelliCampus.Shared.Params;

namespace IntelliCampus.Service;

public class InstructorScheduleService : IInstructorScheduleService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IInstructorService _instructorService;
    private readonly IPdfExportService _pdfExportService;

    public InstructorScheduleService(IUnitOfWork unitOfWork, IInstructorService instructorService, IPdfExportService pdfExportService)
    {
        _unitOfWork = unitOfWork;
        _instructorService = instructorService;
        _pdfExportService = pdfExportService;
    }

    private IGenericRepository<Class, int> Classes
        => _unitOfWork.GetRepository<Class, int>();

    private IGenericRepository<Instructor, int> Instructors
        => _unitOfWork.GetRepository<Instructor, int>();

    public async Task<IEnumerable<ScheduleDto>> GetMyScheduleAsync(int userId, ScheduleQueryParams queryParams)
    {
        var types = queryParams.Types;

        var instructor = await FindInstructorByUserIdAsync(userId);
        var spec = new ClassByInstructorSpec(instructor.UserId);
        var classes = await Classes.GetAllAsync(spec, asNoTracking: true);

        var activeClasses = classes.Where(c => c.Course is null || c.Course.Status == CourseStatus.Active);
        var schedules = activeClasses.Select(MapToDto);

        if (types is not null && types.Length > 0)
        {
            var typeSet = types.ToHashSet();
            schedules = schedules.Where(s => typeSet.Contains(ParseScheduleType(s.Type)));
        }

        return schedules;
    }

    public async Task<IEnumerable<ScheduleDto>> GetScheduleAsync(int instructorId, ScheduleQueryParams queryParams)
    {
        var spec = new ClassByInstructorSpec(instructorId);
        var classes = await Classes.GetAllAsync(spec, asNoTracking: true);
        var activeClasses = classes.Where(c => c.Course is null || c.Course.Status == CourseStatus.Active);
        var schedules = activeClasses.Select(MapToDto);

        var types = queryParams.Types;
        if (types is not null && types.Length > 0)
        {
            var typeSet = types.ToHashSet();
            schedules = schedules.Where(s => typeSet.Contains(ParseScheduleType(s.Type)));
        }

        return schedules;
    }

    public async Task<ScheduleDto> GetScheduleByIdAsync(int classId, int userId)
    {
        var spec = new ClassByIdSpec(classId);
        var cls = await Classes.GetByIdAsync(spec);
        if (cls is null)
            throw new ClassNotFoundException(classId);
        if (cls.InstructorId != userId)
            throw new UnauthorizedAccessException("You do not have access to this class.");

        return MapToDto(cls);
    }

    public async Task<byte[]> ExportSchedulePdfAsync(int userId, ScheduleQueryParams queryParams)
    {
        var schedules = await GetMyScheduleAsync(userId, queryParams);
        var instructorDto = await _instructorService.GetByIdAsync(userId);

        var dto = new ScheduleExportDto
        {
            StudentName = instructorDto?.FullName ?? "",
            StudentCode = instructorDto?.InstructorCode ?? "-",
            Title = "Weekly Schedule",
            Items = schedules.Select(s => new ScheduleItemExportDto
            {
                Day = s.Day,
                StartTime = s.StartTime,
                EndTime = s.EndTime,
                CourseName = s.CourseName ?? s.Title,
                CourseNameAr = s.CourseNameAr,
                Type = s.Type,
                Location = s.Location,
                LocationAr = s.LocationAr,
                InstructorName = s.InstructorName,
                InstructorNameAr = s.InstructorNameAr
            }).ToList()
        };

        return _pdfExportService.ExportSchedule(dto);
    }

    private async Task<Instructor> FindInstructorByUserIdAsync(int userId)
    {
        var spec = new InstructorSpec(userId);
        var instructor = await Instructors.GetByIdAsync(spec);
        return instructor ?? throw new InstructorNotFoundException(userId);
    }

    private static ScheduleDto MapToDto(Class c) => new()
    {
        ScheduleId = c.ClassId,
        Title = c.Course?.CourseName ?? string.Empty,
        TitleAr = c.Course?.CourseNameAr,
        Day = ToDayAbbreviation(c.Day?.ToString() ?? string.Empty),
        Date = DateTime.MinValue,
        StartTime = FormatTime(c.StartTime),
        EndTime = FormatTime(c.EndTime),
        Location = c.Room?.RoomName,
        LocationAr = c.Room?.RoomNameAr,
        Type = c.ClassType switch
        {
            ClassType.Lecture => "lecture",
            ClassType.Section => "section",
            ClassType.Lab => "activity",
            _ => "lecture"
        },
        InstructorName = c.Instructor?.User?.FullName,
        InstructorNameAr = c.Instructor?.User?.FullNameAr,
        CourseId = c.CourseId,
        CourseName = c.Course?.CourseName,
        CourseNameAr = c.Course?.CourseNameAr,
        StudentId = 0,
        RoomId = c.RoomId,
        InstructorId = c.InstructorId
    };

    private static ScheduleType ParseScheduleType(string type) => type.ToLowerInvariant() switch
    {
        "lecture" => ScheduleType.Lecture,
        "section" => ScheduleType.Section,
        "activity" => ScheduleType.Activity,
        _ => ScheduleType.Lecture
    };

    private static string ToDayAbbreviation(string day) => day?.ToLowerInvariant() switch
    {
        "saturday" or "sat" => "sat",
        "sunday" or "sun" => "sun",
        "monday" or "mon" => "mon",
        "tuesday" or "tue" => "tue",
        "wednesday" or "wed" => "wed",
        "thursday" or "thu" => "thu",
        "friday" or "fri" => "fri",
        _ => day?.ToLowerInvariant() ?? string.Empty
    };

    private static string FormatTime(TimeSpan? time) =>
        time.HasValue ? EgyptTime.Today.Add(time.Value).ToString("hh:mm tt") : string.Empty;
}
