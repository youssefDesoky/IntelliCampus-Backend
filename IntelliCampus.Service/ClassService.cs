using System.Globalization;
using System.Linq;
using IntelliCampus.Shared.Dtos.Class;
using IntelliCampus.Shared.Dtos.Instructor;
using IntelliCampus.Shared.Dtos.Room;
using IntelliCampus.Service_Abstraction;
using IntelliCampus.Shared.Params;
using IntelliCampus.Domain.Entities;
using IntelliCampus.Domain.Entities.Enums;
using IntelliCampus.Domain.Interfaces;
using IntelliCampus.Service.Exceptions;
using IntelliCampus.Service.Specifications;

namespace IntelliCampus.Service;

public class ClassService(IUnitOfWork unitOfWork, IScheduleService scheduleService, ICurrentAdminContext adminContext) : IClassService
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly IScheduleService _scheduleService = scheduleService;
    private readonly ICurrentAdminContext _adminContext = adminContext;

    private IGenericRepository<Class, int> Classes
        => _unitOfWork.GetRepository<Class, int>();

    private IGenericRepository<Course, int> Courses
        => _unitOfWork.GetRepository<Course, int>();

    private IGenericRepository<Instructor, int> Instructors
        => _unitOfWork.GetRepository<Instructor, int>();

    private IGenericRepository<Room, int> Rooms
        => _unitOfWork.GetRepository<Room, int>();

    private IGenericRepository<StudentCourse, (int, int)> StudentCourses
        => _unitOfWork.GetRepository<StudentCourse, (int, int)>();

    private async Task EnsureCourseActiveAsync(int courseId)
    {
        var course = await Courses.GetByIdAsync(courseId);
        if (course is null) throw new CourseNotFoundException(courseId);
        if (course.Status != CourseStatus.Active)
            throw new InvalidOperationException("This course is finalized and read-only.");
    }

    public async Task<ClassDto?> GetByIdAsync(int classId)
    {
        var spec = new ClassSpec(classId);
        var classEntity = await Classes.GetByIdAsync(spec);

        if (classEntity is null)
            throw new ClassNotFoundException(classId);

        return MapToDto(classEntity);
    }

    public async Task<IEnumerable<ClassDto>> GetAllAsync(ClassQueryParams? queryParams = null)
    {
        var paramsToUse = queryParams ?? new ClassQueryParams();
        if (_adminContext.IsAdmin)
            paramsToUse.FacultyId = await _adminContext.GetFacultyIdAsync();

        var spec = new ClassSpec(paramsToUse);
        var classes = await Classes.GetAllAsync(spec, asNoTracking: true);
        return classes.Select(MapToDto);
    }

    public async Task<IEnumerable<ClassDto>> GetByCourseIdAsync(int courseId, ClassQueryParams queryParams)
    {
        var course = await Courses.GetByIdAsync(courseId);
        if (course is null)
            throw new CourseNotFoundException(courseId);

        var spec = new ClassSpec(courseId, byCourse: true, queryParams);
        var classes = await Classes.GetAllAsync(spec, asNoTracking: true);
        var courseStudentCount = await StudentCourses.CountAsync(sc => sc.CourseId == courseId);
        return classes.Select(c => MapToDto(c, courseStudentCount));
    }

    public async Task<ClassDto> CreateAsync(CreateClassDto dto)
    {
        // Parse class type from string
        if (!Enum.TryParse<ClassType>(dto.Type, ignoreCase: true, out var classType))
            throw new InvalidOperationException($"Invalid class type '{dto.Type}'. Valid values: Lecture, Section.");

        // Validate course exists and load department
        var courseSpec = new CourseForClassSpec(dto.CourseId);
        var course = await Courses.GetByIdAsync(courseSpec);

        if (course is null)
            throw new CourseNotFoundException(dto.CourseId);

        if (course.Status != CourseStatus.Active)
            throw new InvalidOperationException("This course is finalized and read-only.");

        // Only two Lecture classes per course
        if (classType == ClassType.Lecture)
        {
            var count = await Classes.CountAsync(c => c.CourseId == dto.CourseId && c.ClassType == ClassType.Lecture);

            if (count >= 2)
                throw new InvalidOperationException("Maximum of two lectures allowed per course.");
        }

        // Resolve instructor by name
        int? instructorId = null;
        if (!string.IsNullOrWhiteSpace(dto.InstructorName))
        {
            var spec = new InstructorByNameSpec(dto.InstructorName);
            var instructor = await Instructors.GetByIdAsync(spec);

            if (instructor is null)
                throw new InstructorNotFoundException($"Instructor '{dto.InstructorName}' not found.");

            ValidateInstructorRoleForClassType(instructor, classType);
            instructorId = instructor.UserId;
        }

        // Parse schedule string (e.g. "Thu 05:42")
        DayOfWeekEnum? day = null;
        TimeSpan? startTime = null;

        if (!string.IsNullOrWhiteSpace(dto.Schedule))
        {
            ParseSchedule(dto.Schedule, out day, out startTime);
        }

        var endTime = startTime.HasValue ? AddDuration(startTime.Value) : (TimeSpan?)null;
        await ValidateNoTimeOverlapAsync(dto.CourseId, classType, day, startTime, endTime);

        if (instructorId.HasValue)
            await ValidateInstructorTimeConflictAsync(instructorId.Value, day, startTime, endTime);

        await ValidateCapacityAgainstRoomAsync(dto.RoomId, dto.Capacity);

        // Generate group code (e.g. CS-L1, IS-S1, IS-S2)
        var groupCode = await GenerateGroupCodeAsync(course, classType);

        var classEntity = new Class
        {
            GroupCode = groupCode,
            ClassType = classType,
            Day = day,
            StartTime = startTime,
            EndTime = startTime.HasValue ? AddDuration(startTime.Value) : null,
            RoomId = dto.RoomId,
            CourseId = dto.CourseId,
            InstructorId = instructorId,
            Capacity = dto.Capacity
        };

        Classes.Add(classEntity);
        await _unitOfWork.SaveChangesAsync();

        var reloadSpec = new ClassSpec(classEntity.ClassId);
        var reloadedClass = await Classes.GetByIdAsync(reloadSpec);

        return MapToDto(reloadedClass!);
    }

    public async Task<ClassDto> CreateLectureAsync(CreateLectureDto dto)
    {
        return await CreateInternalAsync(dto.CourseId, dto.InstructorName, dto.Schedule, dto.RoomId, ClassType.Lecture, dto.Capacity);
    }

    public async Task<ClassDto> CreateSectionAsync(CreateSectionDto dto)
    {
        return await CreateInternalAsync(dto.CourseId, dto.InstructorName, dto.Schedule, dto.RoomId, ClassType.Section, dto.Capacity);
    }

    private async Task<ClassDto> CreateInternalAsync(int courseId, string? instructorName, string? schedule, int? roomId, ClassType classType, int? capacity = null)
    {
        var courseSpec = new CourseForClassSpec(courseId);
        var course = await Courses.GetByIdAsync(courseSpec);

        if (course is null)
            throw new CourseNotFoundException(courseId);

        if (course.Status != CourseStatus.Active)
            throw new InvalidOperationException("This course is finalized and read-only.");

        if (classType == ClassType.Lecture)
        {
            var count = await Classes.CountAsync(c => c.CourseId == courseId && c.ClassType == ClassType.Lecture);
            if (count >= 2)
                throw new InvalidOperationException("Maximum of two lectures allowed per course.");
        }

        int? instructorId = null;
        if (!string.IsNullOrWhiteSpace(instructorName))
        {
            var spec = new InstructorByNameSpec(instructorName);
            var instructor = await Instructors.GetByIdAsync(spec);

            if (instructor is null)
                throw new InstructorNotFoundException($"Instructor '{instructorName}' not found.");

            ValidateInstructorRoleForClassType(instructor, classType);
            instructorId = instructor.UserId;
        }

        DayOfWeekEnum? day = null;
        TimeSpan? startTime = null;
        if (!string.IsNullOrWhiteSpace(schedule))
            ParseSchedule(schedule, out day, out startTime);

        var endTime = startTime.HasValue ? AddDuration(startTime.Value) : (TimeSpan?)null;
        await ValidateNoTimeOverlapAsync(courseId, classType, day, startTime, endTime);

        if (instructorId.HasValue)
            await ValidateInstructorTimeConflictAsync(instructorId.Value, day, startTime, endTime);

        await ValidateCapacityAgainstRoomAsync(roomId, capacity);

        var groupCode = await GenerateGroupCodeAsync(course, classType);

        var classEntity = new Class
        {
            GroupCode = groupCode,
            ClassType = classType,
            Day = day,
            StartTime = startTime,
            EndTime = endTime,
            RoomId = roomId,
            CourseId = courseId,
            InstructorId = instructorId,
            Capacity = capacity
        };

        Classes.Add(classEntity);
        await _unitOfWork.SaveChangesAsync();

        var reloadSpec = new ClassSpec(classEntity.ClassId);
        var reloadedClass = await Classes.GetByIdAsync(reloadSpec);
        return MapToDto(reloadedClass!);
    }

    public async Task<ClassDto?> UpdateAsync(int classId, UpdateClassDto dto)
    {
        var spec = new ClassSpec(classId);
        var classEntity = await Classes.GetByIdAsync(spec);

        if (classEntity is null)
            throw new ClassNotFoundException(classId);

        if (classEntity.Course?.Status != CourseStatus.Active)
            throw new InvalidOperationException("This course is finalized and read-only.");

        DayOfWeekEnum? day = null;
        TimeSpan? startTime = null;

        if (dto.Schedule is not null)
            ParseSchedule(dto.Schedule, out day, out startTime);
        else if (dto.Day.HasValue || dto.StartTime.HasValue || dto.EndTime.HasValue)
        {
            if (dto.Day.HasValue)
                classEntity.Day = dto.Day.Value;
            if (dto.StartTime.HasValue)
            {
                classEntity.StartTime = dto.StartTime.Value;
                classEntity.EndTime = dto.EndTime ?? AddDuration(dto.StartTime.Value);
            }
            if (dto.EndTime.HasValue && !dto.StartTime.HasValue)
                classEntity.EndTime = dto.EndTime.Value;
        }

        if (dto.Schedule is not null)
        {
            classEntity.Day = day;
            classEntity.StartTime = startTime;
            classEntity.EndTime = startTime.HasValue ? AddDuration(startTime.Value) : null;
        }

        if (dto.RoomId.HasValue)
            classEntity.RoomId = dto.RoomId;

        if (dto.InstructorId.HasValue)
        {
            var instructor = await Instructors.GetByIdAsync(dto.InstructorId.Value);
            if (instructor is null)
                throw new InstructorNotFoundException(dto.InstructorId.Value);
            classEntity.InstructorId = dto.InstructorId.Value;
        }

        if (dto.Capacity.HasValue)
            classEntity.Capacity = dto.Capacity.Value;

        await ValidateCapacityAgainstRoomAsync(classEntity.RoomId, classEntity.Capacity);

        if (classEntity.InstructorId.HasValue && classEntity.Day is not null && classEntity.StartTime is not null && classEntity.EndTime is not null)
            await ValidateInstructorTimeConflictAsync(classEntity.InstructorId.Value, classEntity.Day, classEntity.StartTime, classEntity.EndTime, excludeClassId: classId);

        Classes.Update(classEntity);
        await _unitOfWork.SaveChangesAsync();

        await _scheduleService.SyncFromClassUpdateAsync(classId);

        var reloadSpec = new ClassSpec(classId);
        var reloadedClass = await Classes.GetByIdAsync(reloadSpec);

        return MapToDto(reloadedClass!);
    }

    public async Task<ClassDto?> AssignInstructorAsync(int classId, int instructorId)
    {
        var spec = new ClassSpec(classId);
        var classEntity = await Classes.GetByIdAsync(spec);

        if (classEntity is null)
            throw new ClassNotFoundException(classId);

        if (classEntity.Course?.Status != CourseStatus.Active)
            throw new InvalidOperationException("This course is finalized and read-only.");

        var instructor = await Instructors.GetByIdAsync(instructorId);
        if (instructor is null)
            throw new InstructorNotFoundException(instructorId);

        ValidateInstructorRoleForClassType(instructor, classEntity.ClassType);

        if (classEntity.Day is not null && classEntity.StartTime is not null && classEntity.EndTime is not null)
            await ValidateInstructorTimeConflictAsync(instructorId, classEntity.Day, classEntity.StartTime, classEntity.EndTime, excludeClassId: classId);

        classEntity.InstructorId = instructorId;
        Classes.Update(classEntity);
        await _unitOfWork.SaveChangesAsync();

        await _scheduleService.SyncFromClassUpdateAsync(classId);

        var reloadSpec = new ClassSpec(classId);
        var reloadedClass = await Classes.GetByIdAsync(reloadSpec);

        return MapToDto(reloadedClass!);
    }

    public async Task<bool> DeleteAsync(int classId)
    {
        var classEntity = await Classes.GetByIdAsync(classId);

        if (classEntity is null)
            throw new ClassNotFoundException(classId);

        await EnsureCourseActiveAsync(classEntity.CourseId);

        var studentCourses = await StudentCourses.GetAllAsync(
            new StudentCourseIdsSpec(classId, byClass: ""), asNoTracking: false);

        foreach (var sc in studentCourses)
        {
            sc.ClassId = null;
            StudentCourses.Update(sc);
        }

        Classes.Delete(classEntity);
        await _unitOfWork.SaveChangesAsync();

        return true;
    }

    public async Task<IEnumerable<InstructorDto>> GetLectureInstructorsAsync(ClassQueryParams? queryParams = null)
    {
        var spec = queryParams is not null
            ? new InstructorSpec([InstructorRole.Professor, InstructorRole.Lecturer, InstructorRole.AssociateProfessor], queryParams)
            : new InstructorSpec(InstructorRole.Professor, InstructorRole.Lecturer, InstructorRole.AssociateProfessor);
        var instructors = await Instructors.GetAllAsync(spec, asNoTracking: true);
        return instructors.Select(MapInstructorToDto);
    }

    public async Task<IEnumerable<InstructorDto>> GetSectionInstructorsAsync(ClassQueryParams? queryParams = null)
    {
        var spec = queryParams is not null
            ? new InstructorSpec([InstructorRole.TeachingAssistant, InstructorRole.AssistantLecturer], queryParams)
            : new InstructorSpec(InstructorRole.TeachingAssistant, InstructorRole.AssistantLecturer);
        var instructors = await Instructors.GetAllAsync(spec, asNoTracking: true);
        return instructors.Select(MapInstructorToDto);
    }

    public async Task<IEnumerable<ClassDto>> GetProfessorLecturesAsync(ClassQueryParams? queryParams = null)
    {
        var spec = queryParams is not null ? new ProfessorLecturesSpec(queryParams) : new ProfessorLecturesSpec();
        var classes = await Classes.GetAllAsync(spec, asNoTracking: true);
        return classes.Select(MapToDto);
    }

    public async Task<IEnumerable<ClassDto>> GetTALecturerSectionsAsync(ClassQueryParams queryParams)
    {
        var spec = new TALecturerSectionsSpec(queryParams);
        var classes = await Classes.GetAllAsync(spec, asNoTracking: true);
        return classes.Select(MapToDto);
    }

    public async Task<IEnumerable<RoomDto>> GetLectureRoomsAsync(ClassQueryParams? queryParams = null)
    {
        var spec = queryParams is not null ? new RoomSpec(queryParams.PageSize, queryParams.PageIndex) : new RoomSpec();
        var rooms = await Rooms.GetAllAsync(spec, asNoTracking: true);
        return rooms.Select(MapRoomToDto);
    }

    public async Task<IEnumerable<RoomDto>> GetSectionRoomsAsync(ClassQueryParams? queryParams = null)
    {
        var spec = queryParams is not null ? new RoomSpec(queryParams.PageSize, queryParams.PageIndex) : new RoomSpec();
        var rooms = await Rooms.GetAllAsync(spec, asNoTracking: true);
        return rooms.Select(MapRoomToDto);
    }

    private async Task<string> GenerateGroupCodeAsync(Course course, ClassType classType)
    {
        // Get department code (e.g. "CS", "IS")
        var deptCode = "GEN";
        if (course.Department is not null)
        {
            var parts = course.Department.DepartmentName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            deptCode = string.Concat(parts.Select(p => char.ToUpperInvariant(p[0])));
        }

        // Type prefix: L = Lecture, S = Section
        var typePrefix = classType switch
        {
            ClassType.Lecture => "L",
            ClassType.Section => "S",
            ClassType.Lab => "LB",
            _ => "X"
        };

        // Count existing classes of the same type for this course
        var existingCount = await Classes.CountAsync(c => c.CourseId == course.CourseId && c.ClassType == classType);

        var number = existingCount + 1;

        return $"{deptCode}-{typePrefix}{number}";
    }

    private static void ValidateInstructorRoleForClassType(Instructor instructor, ClassType classType)
    {
        switch (classType)
        {
            case ClassType.Lecture:
                if (instructor.InstructorRole != InstructorRole.Professor
                    && instructor.InstructorRole != InstructorRole.Lecturer
                    && instructor.InstructorRole != InstructorRole.AssociateProfessor)
                    throw new InvalidOperationException("Only a Professor, Lecturer, or AssociateProfessor can be assigned to a Lecture class.");
                break;

            case ClassType.Section:
                if (instructor.InstructorRole != InstructorRole.TeachingAssistant
                    && instructor.InstructorRole != InstructorRole.AssistantLecturer)
                    throw new InvalidOperationException("Only a TA or AssistantLecturer can be assigned to a Section class.");
                break;
        }
    }

    private static TimeSpan AddDuration(TimeSpan startTime, int minutes = 90)
    {
        var endTime = startTime.Add(TimeSpan.FromMinutes(minutes));
        if (endTime.Days > 0)
            throw new InvalidOperationException(
                $"Class start time {startTime:hh\\:mm} is too late. The {minutes}-minute duration would extend past midnight. Please choose an earlier time.");
        return endTime;
    }

    private static void ParseSchedule(string schedule, out DayOfWeekEnum? day, out TimeSpan? startTime)
    {
        day = null;
        startTime = null;

        var parts = schedule.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0) return;

        // Parse day abbreviation
        var dayAbbreviations = new Dictionary<string, DayOfWeekEnum>(StringComparer.OrdinalIgnoreCase)
        {
            ["sun"] = DayOfWeekEnum.Sunday,
            ["sunday"] = DayOfWeekEnum.Sunday,
            ["mon"] = DayOfWeekEnum.Monday,
            ["monday"] = DayOfWeekEnum.Monday,
            ["tue"] = DayOfWeekEnum.Tuesday,
            ["tuesday"] = DayOfWeekEnum.Tuesday,
            ["wed"] = DayOfWeekEnum.Wednesday,
            ["wednesday"] = DayOfWeekEnum.Wednesday,
            ["thu"] = DayOfWeekEnum.Thursday,
            ["thursday"] = DayOfWeekEnum.Thursday,
            ["fri"] = DayOfWeekEnum.Friday,
            ["friday"] = DayOfWeekEnum.Friday,
            ["sat"] = DayOfWeekEnum.Saturday,
            ["saturday"] = DayOfWeekEnum.Saturday
        };

        if (dayAbbreviations.TryGetValue(parts[0], out var parsedDay))
            day = parsedDay;

        // Parse time
        if (parts.Length > 1)
        {
            if (TimeSpan.TryParse(parts[1], CultureInfo.InvariantCulture, out var time))
                startTime = time;
            else if (DateTime.TryParse(parts[1], CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
                startTime = dt.TimeOfDay;
        }
    }

    private static ClassDto MapToDto(Class classEntity, int courseStudentCount = 0)
    {
        return new ClassDto
        {
            ClassId = classEntity.ClassId,
            GroupCode = classEntity.GroupCode,
            GroupCodeAr = classEntity.GroupCodeAr,
            ClassTypeAr = ClassTypeAr(classEntity.ClassType),
            ClassType = classEntity.ClassType,
            Day = classEntity.Day,
            DayNameAr = DayNameAr(classEntity.Day),
            StartTime = classEntity.StartTime,
            EndTime = classEntity.EndTime,
            RoomId = classEntity.RoomId,
            RoomName = classEntity.Room?.RoomName,
            RoomNameAr = classEntity.Room?.RoomNameAr,
            Capacity = classEntity.Capacity,
            EnrolledCount = classEntity.ClassType == ClassType.Lecture
                ? courseStudentCount
                : classEntity.StudentCourses?.Count(sc => sc.Status == StudentCourseStatus.InProgress) ?? 0,
            CourseId = classEntity.CourseId,
            CourseName = classEntity.Course.CourseName,
            CourseNameAr = classEntity.Course?.CourseNameAr,
            InstructorId = classEntity.InstructorId,
            InstructorName = classEntity.Instructor?.User?.FullName,
            InstructorNameAr = classEntity.Instructor?.User?.FullNameAr
        };
    }

    private async Task ValidateCapacityAgainstRoomAsync(int? roomId, int? capacity)
    {
        if (!roomId.HasValue)
            return;

        if (!capacity.HasValue)
            throw new InvalidOperationException("Capacity is required when a room is selected.");

        var room = await Rooms.GetByIdAsync(roomId.Value);
        if (room is null)
            return;

        if (capacity.Value > room.Capacity)
            throw new InvalidOperationException($"Class capacity ({capacity.Value}) exceeds room capacity ({room.Capacity}).");
    }

    private async Task ValidateNoTimeOverlapAsync(int courseId, ClassType classType, DayOfWeekEnum? day, TimeSpan? startTime, TimeSpan? endTime, int? excludeClassId = null)
    {
        if (day == null || startTime == null || endTime == null)
            return;

        var classTypeStr = classType.ToString();
        var existing = await Classes.GetAllAsync(
            new ClassSpec(courseId, byCourse: true, classTypeStr), asNoTracking: true);

        foreach (var cls in existing)
        {
            if (excludeClassId.HasValue && cls.ClassId == excludeClassId.Value)
                continue;
            if (cls.Day != day.Value || cls.StartTime == null || cls.EndTime == null)
                continue;

            if (startTime.Value < cls.EndTime.Value && endTime.Value > cls.StartTime.Value)
            {
                var typeName = classTypeStr.ToLower();
                var dayName = day.Value.ToString();
                throw new InvalidOperationException(
                    $"Time conflict: new {typeName} on {dayName} overlaps with an existing {typeName}. Please choose a different time or day.");
            }
        }
    }

    private async Task ValidateInstructorTimeConflictAsync(int instructorId, DayOfWeekEnum? day, TimeSpan? startTime, TimeSpan? endTime, int? excludeClassId = null)
    {
        if (day is null || startTime is null || endTime is null)
            return;

        var existing = await Classes.GetAllAsync(
            new ClassByInstructorSpec(instructorId), asNoTracking: true);

        foreach (var cls in existing)
        {
            if (excludeClassId.HasValue && cls.ClassId == excludeClassId.Value)
                continue;
            if (cls.Day != day.Value || cls.StartTime is null || cls.EndTime is null)
                continue;

            if (startTime.Value < cls.EndTime.Value && endTime.Value > cls.StartTime.Value)
            {
                var dayName = day.Value.ToString();
                throw new InvalidOperationException(
                    $"Instructor already has a class scheduled on {dayName} at {cls.StartTime.Value:hh\\:mm}–{cls.EndTime.Value:hh\\:mm}. Please choose a different time or assign a different instructor.");
            }
        }
    }

    private static InstructorDto MapInstructorToDto(Instructor instructor)
    {
        return new InstructorDto
        {
            InstructorId = instructor.UserId,
            UserId = instructor.UserId,
            NationalId = instructor.User.NationalId,
            FullName = instructor.User.FullName,
            FullNameAr = instructor.User.FullNameAr,
            PhoneNumber = instructor.User.PhoneNumber,
            Email = instructor.User.Email,
            Address = instructor.User.Address,
            Nationality = instructor.User.Nationality,
            InstructorCode = instructor.InstructorCode,
            InstructorRole = instructor.InstructorRole?.ToString(),
            DepartmentId = instructor.DepartmentId,
            DepartmentName = instructor.Department?.DepartmentName,
            DepartmentNameAr = instructor.Department?.DepartmentNameAr,
            HireDate = instructor.HireDate?.ToString("dd MM yyyy"),
            FacultyId = instructor.User.FacultyId,
            FacultyName = instructor.User.Faculty?.FacultyName,
            FacultyNameAr = instructor.User.Faculty?.FacultyNameAr,
            Status = instructor.Status?.ToString(),
            OfficeHoursRoomId = instructor.OfficeHoursRoomId,
            OfficeHoursRoomName = instructor.OfficeHoursRoom?.RoomName,
            OfficeHoursRoomNameAr = instructor.OfficeHoursRoom?.RoomNameAr,
            ContractStartDate = instructor.ContractStartDate?.ToString("dd MM yyyy"),
            ContractEndDate = instructor.ContractEndDate?.ToString("dd MM yyyy"),
            Secondment = instructor.Secondment,
            Roles = instructor.User.UserRoles.Where(ur => ur.IsActive).Select(ur => ur.Role.RoleName).ToList()
        };
    }

    private static string? ClassTypeAr(ClassType type) => type switch
    {
        ClassType.Lecture => "محاضرة",
        ClassType.Lab => "معمل",
        ClassType.Section => "مجموعة",
        _ => null
    };

    private static string? DayNameAr(DayOfWeekEnum? day) => day switch
    {
        DayOfWeekEnum.Sunday => "الأحد",
        DayOfWeekEnum.Monday => "الإثنين",
        DayOfWeekEnum.Tuesday => "الثلاثاء",
        DayOfWeekEnum.Wednesday => "الأربعاء",
        DayOfWeekEnum.Thursday => "الخميس",
        DayOfWeekEnum.Friday => "الجمعة",
        DayOfWeekEnum.Saturday => "السبت",
        _ => null
    };

    private static RoomDto MapRoomToDto(Room room)
    {
        return new RoomDto
        {
            RoomId = room.RoomId,
            RoomName = room.RoomName,
            RoomNameAr = room.RoomNameAr,
            Capacity = room.Capacity,
            Type = room.Type,
            Location = room.Location,
            LocationAr = room.LocationAr
        };
    }
}