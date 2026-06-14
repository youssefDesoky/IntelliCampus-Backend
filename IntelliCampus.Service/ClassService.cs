using System.Globalization;
using IntelliCampus.Shared.Dtos.Class;
using IntelliCampus.Service_Abstraction;
using IntelliCampus.Domain.Entities;
using IntelliCampus.Domain.Entities.Enums;
using IntelliCampus.Domain.Interfaces;
using IntelliCampus.Service.Specifications;

namespace IntelliCampus.Service;

public class ClassService(IUnitOfWork unitOfWork) : IClassService
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;

    private IGenericRepository<Class, int> Classes
        => _unitOfWork.GetRepository<Class, int>();

    private IGenericRepository<Course, int> Courses
        => _unitOfWork.GetRepository<Course, int>();

    private IGenericRepository<Instructor, int> Instructors
        => _unitOfWork.GetRepository<Instructor, int>();

    public async Task<ClassDto?> GetByIdAsync(int classId)
    {
        var spec = new ClassSpec(classId);
        var classEntity = await Classes.GetByIdAsync(spec);

        if (classEntity is null)
            return null;

        return MapToDto(classEntity);
    }

    public async Task<IEnumerable<ClassDto>> GetAllAsync()
    {
        var spec = new ClassSpec();
        var classes = await Classes.GetAllAsync(spec);

        return classes.Select(MapToDto);
    }

    public async Task<IEnumerable<ClassDto>> GetByCourseIdAsync(int courseId)
    {
        var spec = new ClassSpec(courseId, byCourse: true);
        var classes = await Classes.GetAllAsync(spec);

        return classes.Select(MapToDto);
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
            throw new InvalidOperationException("Course not found.");

        // Only one Lecture class per course
        if (classType == ClassType.Lecture)
        {
            var count = await Classes.CountAsync(c => c.CourseId == dto.CourseId && c.ClassType == ClassType.Lecture);

            if (count > 0)
                throw new InvalidOperationException("A lecture class already exists for this course. Only one lecture is allowed per course.");
        }

        // Resolve instructor by name
        int? instructorId = null;
        if (!string.IsNullOrWhiteSpace(dto.InstructorName))
        {
            var spec = new InstructorByNameSpec(dto.InstructorName);
            var instructor = await Instructors.GetByIdAsync(spec);

            if (instructor is null)
                throw new InvalidOperationException($"Instructor '{dto.InstructorName}' not found.");

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

        // Generate group code (e.g. CS-L1, IS-S1, IS-S2)
        var groupCode = await GenerateGroupCodeAsync(course, classType);

        var classEntity = new Class
        {
            GroupCode = groupCode,
            ClassType = classType,
            Day = day,
            StartTime = startTime,
            EndTime = startTime.HasValue ? startTime.Value.Add(TimeSpan.FromMinutes(90)) : null,
            Room = dto.Room,
            CourseId = dto.CourseId,
            InstructorId = instructorId
        };

        Classes.Add(classEntity);
        await _unitOfWork.SaveChangesAsync();

        var reloadSpec = new ClassSpec(classEntity.ClassId);
        var reloadedClass = await Classes.GetByIdAsync(reloadSpec);

        return MapToDto(reloadedClass!);
    }

    public async Task<ClassDto?> AssignInstructorAsync(int classId, int instructorId)
    {
        var spec = new ClassSpec(classId);
        var classEntity = await Classes.GetByIdAsync(spec);

        if (classEntity is null)
            return null;

        var instructor = await Instructors.GetByIdAsync(instructorId);
        if (instructor is null)
            throw new InvalidOperationException("Instructor not found.");

        ValidateInstructorRoleForClassType(instructor, classEntity.ClassType);

        classEntity.InstructorId = instructorId;
        Classes.Update(classEntity);
        await _unitOfWork.SaveChangesAsync();

        var reloadSpec = new ClassSpec(classId);
        var reloadedClass = await Classes.GetByIdAsync(reloadSpec);

        return MapToDto(reloadedClass!);
    }

    public async Task<bool> DeleteAsync(int classId)
    {
        var classEntity = await Classes.GetByIdAsync(classId);

        if (classEntity is null)
            return false;

        Classes.Delete(classEntity);
        await _unitOfWork.SaveChangesAsync();

        return true;
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
                if (instructor.InstructorRole != InstructorRole.Professor)
                    throw new InvalidOperationException("Only a Professor can be assigned to a Lecture class.");
                break;

            case ClassType.Section:
                if (instructor.InstructorRole != InstructorRole.TeachingAssistant)
                    throw new InvalidOperationException("Only a TA can be assigned to a Section class.");
                break;
        }
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

    private static ClassDto MapToDto(Class classEntity)
    {
        return new ClassDto
        {
            ClassId = classEntity.ClassId,
            GroupCode = classEntity.GroupCode,
            ClassType = classEntity.ClassType,
            Day = classEntity.Day,
            StartTime = classEntity.StartTime,
            EndTime = classEntity.EndTime,
            Room = classEntity.Room,
            CourseId = classEntity.CourseId,
            CourseName = classEntity.Course.CourseName,
            InstructorId = classEntity.InstructorId,
            InstructorName = classEntity.Instructor?.FullName
        };
    }
}
