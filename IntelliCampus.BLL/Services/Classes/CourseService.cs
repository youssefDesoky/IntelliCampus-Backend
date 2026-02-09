using IntelliCampus.BLL.Dtos.Course;
using IntelliCampus.BLL.Services.Interfaces;
using IntelliCampus.BLL.Utilities;
using IntelliCampus.DAL.Data.Contexts;
using IntelliCampus.DAL.Entities;
using IntelliCampus.DAL.Entities.Enums;
using Microsoft.EntityFrameworkCore;

namespace IntelliCampus.BLL.Services.Classes;

public class CourseService : ICourseService
{
    private const int TotalSemesterWeeks = 16;

    private readonly IntelliCampusDbContext _context;

    public CourseService(IntelliCampusDbContext context)
    {
        _context = context;
    }

    public async Task<CourseDto?> GetByIdAsync(int courseId)
    {
        var course = await GetCourseQuery()
            .FirstOrDefaultAsync(c => c.CourseId == courseId);

        if (course is null)
            return null;

        return MapToDto(course);
    }

    public async Task<IEnumerable<CourseDto>> GetAllAsync()
    {
        var courses = await GetCourseQuery().ToListAsync();
        return courses.Select(MapToDto);
    }

    public async Task<IEnumerable<CourseDto>> GetActiveCoursesAsync()
    {
        var courses = await GetCourseQuery()
            .Where(c => c.Status == CourseStatus.Active)
            .ToListAsync();

        return courses.Select(MapToDto);
    }

    public async Task<IEnumerable<CourseDto>> GetCoursesByStudentIdAsync(int studentId)
    {
        var courseIds = await _context.StudentCourses
            .Where(sc => sc.StudentId == studentId)
            .Select(sc => sc.CourseId)
            .ToListAsync();

        var courses = await GetCourseQuery()
            .Where(c => courseIds.Contains(c.CourseId))
            .ToListAsync();

        return courses.Select(MapToDto);
    }

    public async Task<IEnumerable<CourseDto>> GetCoursesByInstructorIdAsync(int instructorId)
    {
        var courseIds = await _context.Classes
            .Where(c => c.InstructorId == instructorId)
            .Select(c => c.CourseId)
            .Distinct()
            .ToListAsync();

        var courses = await GetCourseQuery()
            .Where(c => courseIds.Contains(c.CourseId))
            .ToListAsync();

        return courses.Select(MapToDto);
    }

    public async Task<CourseDto> CreateAsync(CreateCourseDto dto)
    {
        var departmentId = await ResolveDepartmentIdAsync(dto.DepartmentName);

        var course = new Course
        {
            CourseCode = dto.CourseCode,
            CourseName = dto.CourseName,
            CourseNameAr = dto.CourseNameAr,
            CreditHours = dto.CreditHours,
            Status = CourseStatus.Active,
            DepartmentId = departmentId
        };

        _context.Courses.Add(course);
        await _context.SaveChangesAsync();

        if (dto.PrerequisiteCodes is { Count: > 0 })
        {
            var prereqCourses = await _context.Courses
                .Where(c => dto.PrerequisiteCodes.Contains(c.CourseCode!))
                .ToListAsync();

            foreach (var prereq in prereqCourses)
            {
                _context.Set<CoursePrerequisite>().Add(new CoursePrerequisite
                {
                    CourseId = course.CourseId,
                    PrerequisiteCourseId = prereq.CourseId
                });
            }

            await _context.SaveChangesAsync();
        }

        if (course.DepartmentId.HasValue)
            await _context.Entry(course).Reference(c => c.Department).LoadAsync();
        await _context.Entry(course).Collection(c => c.Prerequisites).LoadAsync();

        return MapToDto(course);
    }

    public async Task<bool> ActivateAsync(int courseId)
    {
        var course = await _context.Courses.FindAsync(courseId);
        if (course is null) return false;
        course.Status = CourseStatus.Active;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeactivateAsync(int courseId)
    {
        var course = await _context.Courses.FindAsync(courseId);
        if (course is null) return false;
        course.Status = CourseStatus.Inactive;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteAsync(int courseId)
    {
        var course = await _context.Courses.FindAsync(courseId);
        if (course is null) return false;
        _context.Courses.Remove(course);
        await _context.SaveChangesAsync();
        return true;
    }

    private IQueryable<Course> GetCourseQuery()
    {
        return _context.Courses
            .Include(c => c.Department)
            .Include(c => c.Classes)
                .ThenInclude(cl => cl.Instructor)
            .Include(c => c.StudentCourses)
            .Include(c => c.Grades)
            .Include(c => c.Prerequisites)
                .ThenInclude(p => p.PrerequisiteCourse)
            .Include(c => c.Classes)
                .ThenInclude(cl => cl.Sessions)
                    .ThenInclude(s => s.Attendances);
    }

    private async Task<int?> ResolveDepartmentIdAsync(string? departmentName)
    {
        if (string.IsNullOrWhiteSpace(departmentName))
            return null;

        if (int.TryParse(departmentName, out var id))
        {
            if (await _context.Departments.AnyAsync(d => d.DepartmentId == id))
                return id;
        }

        var department = await _context.Departments
            .FirstOrDefaultAsync(d => d.DepartmentName.ToLower() == departmentName.ToLower());

        if (department is not null)
            return department.DepartmentId;

        var normalized = departmentName.Trim();
        var departments = await _context.Departments.ToListAsync();
        var matched = departments.FirstOrDefault(d =>
            string.Equals(GetDepartmentCode(d.DepartmentName), normalized, StringComparison.OrdinalIgnoreCase));

        if (matched is null)
            throw new InvalidOperationException("Department not found.");

        return matched.DepartmentId;
    }

    private static string GetDepartmentCode(string departmentName)
    {
        var parts = departmentName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return string.Concat(parts.Select(p => char.ToUpperInvariant(p[0])));
    }

    private static CourseDto MapToDto(Course course)
    {
        var currentSemester = SemesterHelper.GetCurrentSemester();

        var allSessions = course.Classes?.SelectMany(cl => cl.Sessions) ?? [];
        var allAttendances = allSessions.SelectMany(s => s.Attendances);
        var totalAttendances = allAttendances.Count();
        var presentAttendances = allAttendances.Count(a => a.Status == AttendanceStatus.Present || a.Status == AttendanceStatus.Late);
        var attendancePercent = totalAttendances > 0 ? Math.Round((decimal)presentAttendances / totalAttendances * 100, 1) : (decimal?)null;

        var courseGrades = course.Grades ?? [];
        var avgGrade = courseGrades.Any() ? Math.Round(courseGrades.Average(g => g.Score), 1) : (decimal?)null;

        var numStudents = course.StudentCourses?.Count ?? 0;

        var lectureClass = course.Classes?.FirstOrDefault(cl => cl.ClassType == ClassType.Lecture);
        var scheduleClass = lectureClass ?? course.Classes?.FirstOrDefault();

        string? schedule = null;
        string? room = null;

        if (scheduleClass is not null)
        {
            room = scheduleClass.Room;
            if (scheduleClass.Day.HasValue && scheduleClass.StartTime.HasValue && scheduleClass.EndTime.HasValue)
            {
                var startFormatted = DateTime.Today.Add(scheduleClass.StartTime.Value).ToString("h:mm tt");
                var endFormatted = DateTime.Today.Add(scheduleClass.EndTime.Value).ToString("h:mm tt");
                schedule = $"{scheduleClass.Day.Value} {startFormatted} - {endFormatted}";
            }
        }

        var distinctSessionWeeks = allSessions
            .Select(s => s.Date)
            .Where(d => d <= DateTime.UtcNow)
            .Select(d => System.Globalization.CultureInfo.InvariantCulture.Calendar
                .GetWeekOfYear(d, System.Globalization.CalendarWeekRule.FirstDay, DayOfWeek.Sunday))
            .Distinct()
            .Count();

        return new CourseDto
        {
            CourseId = course.CourseId,
            CourseCode = course.CourseCode,
            CourseName = course.CourseName,
            CourseNameAr = course.CourseNameAr,
            CreditHours = course.CreditHours,
            Status = course.Status,
            DepartmentId = course.DepartmentId,
            DepartmentName = course.Department?.DepartmentName,
            ClassCount = course.Classes?.Count ?? 0,
            Prerequisites = course.Prerequisites?
                .Select(p => p.PrerequisiteCourse?.CourseCode ?? p.PrerequisiteCourseId.ToString())
                .ToList(),
            Semester = currentSemester,
            Schedule = schedule,
            Room = room,
            NumOfStudents = numStudents,
            TotalStudents = numStudents,
            WeeksCompleted = distinctSessionWeeks,
            Weeks = TotalSemesterWeeks,
            Attendance = attendancePercent,
            Grade = avgGrade,
            IsElective = false,
            ProfessorName = lectureClass?.Instructor?.FullName
        };
    }
}
