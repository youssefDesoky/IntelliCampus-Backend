using System.Globalization;
using IntelliCampus.BLL.Dtos.Class;
using IntelliCampus.BLL.Services.Interfaces;
using IntelliCampus.DAL.Data.Contexts;
using IntelliCampus.DAL.Entities;
using IntelliCampus.DAL.Entities.Enums;
using Microsoft.EntityFrameworkCore;

namespace IntelliCampus.BLL.Services.Classes;

public class ClassService : IClassService
{
    private readonly IntelliCampusDbContext _context;

    public ClassService(IntelliCampusDbContext context)
    {
        _context = context;
    }

    public async Task<ClassDto?> GetByIdAsync(int classId)
    {
        var classEntity = await _context.Classes
            .Include(c => c.Course)
            .Include(c => c.Instructor)
            .FirstOrDefaultAsync(c => c.ClassId == classId);

        if (classEntity is null)
            return null;

        return MapToDto(classEntity);
    }

    public async Task<IEnumerable<ClassDto>> GetAllAsync()
    {
        var classes = await _context.Classes
            .Include(c => c.Course)
            .Include(c => c.Instructor)
            .ToListAsync();

        return classes.Select(MapToDto);
    }

    public async Task<IEnumerable<ClassDto>> GetByCourseIdAsync(int courseId)
    {
        var classes = await _context.Classes
            .Include(c => c.Course)
            .Include(c => c.Instructor)
            .Where(c => c.CourseId == courseId)
            .ToListAsync();

        return classes.Select(MapToDto);
    }

    public async Task<ClassDto> CreateAsync(CreateClassDto dto)
    {
        // Parse class type from string
        if (!Enum.TryParse<ClassType>(dto.Type, ignoreCase: true, out var classType))
            throw new InvalidOperationException($"Invalid class type '{dto.Type}'. Valid values: Lecture, Section.");

        // Validate course exists and load department
        var course = await _context.Courses
            .Include(c => c.Department)
            .FirstOrDefaultAsync(c => c.CourseId == dto.CourseId);

        if (course is null)
            throw new InvalidOperationException("Course not found.");

        // Only one Lecture class per course
        if (classType == ClassType.Lecture)
        {
            var lectureExists = await _context.Classes
                .AnyAsync(c => c.CourseId == dto.CourseId && c.ClassType == ClassType.Lecture);

            if (lectureExists)
                throw new InvalidOperationException("A lecture class already exists for this course. Only one lecture is allowed per course.");
        }

        // Resolve instructor by name
        int? instructorId = null;
        if (!string.IsNullOrWhiteSpace(dto.InstructorName))
        {
            var instructor = await _context.Instructors
                .FirstOrDefaultAsync(i => i.FullName.ToLower() == dto.InstructorName.ToLower());

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

        _context.Classes.Add(classEntity);
        await _context.SaveChangesAsync();

        // Reload with related entities
        await _context.Entry(classEntity).Reference(c => c.Course).LoadAsync();
        if (classEntity.InstructorId.HasValue)
            await _context.Entry(classEntity).Reference(c => c.Instructor).LoadAsync();

        return MapToDto(classEntity);
    }

    public async Task<ClassDto?> AssignInstructorAsync(int classId, int instructorId)
    {
        var classEntity = await _context.Classes
            .Include(c => c.Course)
            .FirstOrDefaultAsync(c => c.ClassId == classId);

        if (classEntity is null)
            return null;

        var instructor = await _context.Instructors.FirstOrDefaultAsync(i => i.UserId == instructorId);
        if (instructor is null)
            throw new InvalidOperationException("Instructor not found.");

        ValidateInstructorRoleForClassType(instructor, classEntity.ClassType);

        classEntity.InstructorId = instructorId;
        await _context.SaveChangesAsync();

        await _context.Entry(classEntity).Reference(c => c.Instructor).LoadAsync();

        return MapToDto(classEntity);
    }

    public async Task<bool> DeleteAsync(int classId)
    {
        var classEntity = await _context.Classes.FindAsync(classId);

        if (classEntity is null)
            return false;

        _context.Classes.Remove(classEntity);
        await _context.SaveChangesAsync();

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
        var existingCount = await _context.Classes
            .CountAsync(c => c.CourseId == course.CourseId && c.ClassType == classType);

        var number = existingCount + 1;

        return $"{deptCode}-{typePrefix}{number}";
    }

    private static void ValidateInstructorRoleForClassType(Instructor instructor, ClassType classType)
    {
        var role = instructor.InstructorRole?.ToLower();

        switch (classType)
        {
            case ClassType.Lecture:
                if (role != "professor")
                    throw new InvalidOperationException("Only a Professor can be assigned to a Lecture class.");
                break;

            case ClassType.Section:
                if (role != "ta")
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
