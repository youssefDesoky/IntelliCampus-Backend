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

public class ScheduleService : IScheduleService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IStudentService _studentService;
    private readonly IPdfExportService _pdfExportService;

    public ScheduleService(IUnitOfWork unitOfWork, IStudentService studentService, IPdfExportService pdfExportService)
    {
        _unitOfWork = unitOfWork;
        _studentService = studentService;
        _pdfExportService = pdfExportService;
    }

    private IGenericRepository<Schedule, int> Schedules
        => _unitOfWork.GetRepository<Schedule, int>();

    private IGenericRepository<Class, int> Classes
        => _unitOfWork.GetRepository<Class, int>();

    private IGenericRepository<Student, int> Students
        => _unitOfWork.GetRepository<Student, int>();

    private IGenericRepository<BylawCourse, int> BylawCourses
        => _unitOfWork.GetRepository<BylawCourse, int>();

    private IGenericRepository<StudentCourse, (int StudentId, int CourseId)> StudentCourses
        => _unitOfWork.GetRepository<StudentCourse, (int StudentId, int CourseId)>();

    public async Task<ScheduleDto> GetByIdAsync(int scheduleId)
    {
        var spec = new ScheduleSpec(scheduleId, byId: true);
        var schedule = await Schedules.GetByIdAsync(spec);
        if (schedule is null) throw new ScheduleNotFoundException(scheduleId);
        return MapToDto(schedule);
    }

    public async Task<IEnumerable<ScheduleDto>> GetByStudentIdAsync(int studentId, ScheduleQueryParams? queryParams = null)
    {
        var student = await Students.GetByIdAsync(studentId);
        if (student is null)
            throw new StudentNotFoundException(studentId);

        var bylawCourseIds = await GetBylawCourseIdsAsync(student.BylawId);
        var activeCourseIds = await GetActiveEnrolledCourseIdsAsync(studentId);

        var spec = queryParams is not null
            ? new ScheduleSpec(studentId, queryParams.PageSize, queryParams.PageIndex)
            : new ScheduleSpec(studentId);
        var schedules = await Schedules.GetAllAsync(spec, asNoTracking: true);
        return FilterByBylaw(schedules, bylawCourseIds)
            .Where(s => s.Course is null || (s.Course.Status == CourseStatus.Active && activeCourseIds.Contains(s.CourseId!.Value)))
            .Select(MapToDto);
    }

    public async Task<IEnumerable<ScheduleDto>> GetByStudentIdAndTypeAsync(int studentId, ScheduleType type, ScheduleQueryParams? queryParams = null)
    {
        var student = await Students.GetByIdAsync(studentId);
        if (student is null)
            throw new StudentNotFoundException(studentId);

        var bylawCourseIds = await GetBylawCourseIdsAsync(student.BylawId);
        var activeCourseIds = await GetActiveEnrolledCourseIdsAsync(studentId);

        var spec = queryParams is not null
            ? new ScheduleSpec(studentId, type, queryParams)
            : new ScheduleSpec(studentId, type);
        var schedules = await Schedules.GetAllAsync(spec, asNoTracking: true);
        return FilterByBylaw(schedules, bylawCourseIds)
            .Where(s => s.Course is null || (s.Course.Status == CourseStatus.Active && activeCourseIds.Contains(s.CourseId!.Value)))
            .Select(MapToDto);
    }

    public async Task<IEnumerable<ScheduleDto>> GetByStudentIdAndTypesAsync(int studentId, ScheduleQueryParams queryParams)
    {
        var student = await Students.GetByIdAsync(studentId);
        if (student is null)
            throw new StudentNotFoundException(studentId);

        var bylawCourseIds = await GetBylawCourseIdsAsync(student.BylawId);
        var activeCourseIds = await GetActiveEnrolledCourseIdsAsync(studentId);

        var spec = new ScheduleSpec(studentId, queryParams);
        var schedules = await Schedules.GetAllAsync(spec, asNoTracking: true);
        return FilterByBylaw(schedules, bylawCourseIds)
            .Where(s => s.Course is null || (s.Course.Status == CourseStatus.Active && activeCourseIds.Contains(s.CourseId!.Value)))
            .Select(MapToDto);
    }

    private async Task<List<int>> GetBylawCourseIdsAsync(int? bylawId)
    {
        if (bylawId is null)
            return [];

        return (await BylawCourses.GetAllAsync(new BylawCourseSpec(bylawId.Value, false), asNoTracking: true))
            .Select(bc => bc.CourseId)
            .Distinct()
            .ToList();
    }

    private async Task<HashSet<int>> GetActiveEnrolledCourseIdsAsync(int studentId)
    {
        var enrollments = await StudentCourses.GetAllAsync(
            new StudentCourseIdsSpec(studentId, [StudentCourseStatus.InProgress, StudentCourseStatus.Registered]),
            asNoTracking: true);

        return enrollments.Select(sc => sc.CourseId).ToHashSet();
    }

    private static IEnumerable<Schedule> FilterByBylaw(IEnumerable<Schedule> schedules, List<int> bylawCourseIds)
    {
        if (bylawCourseIds.Count == 0)
            return schedules;

        return schedules.Where(s => s.CourseId is null || bylawCourseIds.Contains(s.CourseId.Value));
    }

    public async Task SyncFromCourseRegistrationAsync(int studentId, int classId)
    {
        var cls = await Classes.GetByIdAsync(new ClassByIdSpec(classId));
        if (cls is null)
            throw new ClassNotFoundException(classId);

        if (cls.StartTime is null || cls.EndTime is null)
            throw new InvalidOperationException("Class schedule is not fully defined (StartTime/EndTime).");

        var schedule = new Schedule
        {
            Title = cls.Course.CourseName,
            TitleAr = cls.Course?.CourseNameAr,
            Day = cls.Day?.ToString() ?? string.Empty,
            StartTime = cls.StartTime.Value,
            EndTime = cls.EndTime.Value,
            RoomId = cls.RoomId,
            ScheduleType = cls.ClassType switch
            {
                ClassType.Lecture => ScheduleType.Lecture,
                ClassType.Section => ScheduleType.Section,
                ClassType.Lab => ScheduleType.Activity,
                _ => ScheduleType.Lecture
            },
            InstructorId = cls.InstructorId,
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
            throw new ClassNotFoundException(classId);

        if (cls.StartTime is null || cls.EndTime is null)
            throw new InvalidOperationException("Class schedule is not fully defined (StartTime/EndTime).");

        var schedules = await Schedules.GetAllAsync(new ScheduleByClassIdSpec(classId));
        foreach (var s in schedules)
        {
            s.Day = cls.Day?.ToString() ?? string.Empty;
            s.StartTime = cls.StartTime.Value;
            s.EndTime = cls.EndTime.Value;
            s.RoomId = cls.RoomId;
            s.InstructorId = cls.InstructorId;
            Schedules.Update(s);
        }

        await _unitOfWork.SaveChangesAsync();
    }

    public async Task<byte[]> ExportSchedulePdfAsync(int studentId, ScheduleQueryParams queryParams)
    {
        var student = await _studentService.GetByIdAsync(studentId);

        IEnumerable<ScheduleDto> schedules;
        if (queryParams.Types is null || queryParams.Types.Length == 0)
            schedules = await GetByStudentIdAsync(studentId);
        else
            schedules = await GetByStudentIdAndTypesAsync(studentId, new ScheduleQueryParams { Types = queryParams.Types, PageSize = 50 });

        var dto = new ScheduleExportDto
        {
            StudentName = student?.FullName ?? "",
            StudentCode = student?.StudentCode ?? "-",
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

        try
        {
            return _pdfExportService.ExportSchedule(dto);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"ExportSchedule failed: {ex.Message}", ex);
        }
    }

    private static ScheduleDto MapToDto(Schedule s) => new()
    {
        ScheduleId = s.ScheduleId,
        Title = s.Title,
        TitleAr = s.TitleAr,
        Day = ToDayAbbreviation(s.Day),
        Date = s.Date,
        StartTime = FormatTime(s.StartTime),
        EndTime = FormatTime(s.EndTime),
        Location = s.Room?.RoomName,
        LocationAr = s.Room?.RoomNameAr,
        Type = s.ScheduleType.ToString().ToLowerInvariant(),
        InstructorName = s.Instructor?.User?.FullName,
        InstructorNameAr = s.Instructor?.User?.FullNameAr,
        CourseId = s.CourseId,
        CourseName = s.Course?.CourseName,
        CourseNameAr = s.Course?.CourseNameAr,
        StudentId = s.StudentId,
        RoomId = s.RoomId,
        InstructorId = s.InstructorId
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

    private static string FormatTime(TimeSpan time) =>
        EgyptTime.Today.Add(time).ToString("hh:mm tt");
}
