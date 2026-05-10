using IntelliCampus.Shared.Dtos.Course;
using IntelliCampus.Service_Abstraction;
using IntelliCampus.Domain.Helpers;
using IntelliCampus.Domain.Entities;
using IntelliCampus.Domain.Entities.Enums;
using IntelliCampus.Domain.Interfaces;
using IntelliCampus.Service.Specifications;

namespace IntelliCampus.Service;

public class CourseService(IUnitOfWork unitOfWork) : ICourseService
{
    private const int TotalSemesterWeeks = 16;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;

    private IGenericRepository<Course, int> Courses
        => _unitOfWork.GetRepository<Course, int>();

    private IGenericRepository<StudentCourse, int> StudentCourses
        => _unitOfWork.GetRepository<StudentCourse, int>();

    private IGenericRepository<Class, int> Classes
        => _unitOfWork.GetRepository<Class, int>();

    private IGenericRepository<Department, int> Departments
        => _unitOfWork.GetRepository<Department, int>();

    private IGenericRepository<CoursePrerequisite, int> Prerequisites
        => _unitOfWork.GetRepository<CoursePrerequisite, int>();

    public async Task<CourseDto?> GetByIdAsync(int courseId)
    {
        var course = await Courses.GetByIdAsync(new CourseSpec(courseId));

        if (course is null)
            return null;

        return MapToDto(course);
    }

    public async Task<IEnumerable<CourseDto>> GetAllAsync()
    {
        var courses = await Courses.GetAllAsync(new CourseSpec());
        return courses.Select(MapToDto);
    }

    public async Task<IEnumerable<CourseDto>> GetActiveCoursesAsync()
    {
        var courses = await Courses.GetAllAsync(new CourseSpec(CourseStatus.Active));

        return courses.Select(MapToDto);
    }

    public async Task<IEnumerable<CourseDto>> GetCoursesByStudentIdAsync(int studentId)
    {
        var studentCourses = await StudentCourses.GetAllAsync(new StudentCourseIdsSpec(studentId));
        var courseIds = studentCourses.Select(sc => sc.CourseId).ToList();

        var courses = await Courses.GetAllAsync(new CourseSpec(courseIds));

        return courses.Select(MapToDto);
    }

    public async Task<IEnumerable<CourseDto>> GetCoursesByInstructorIdAsync(int instructorId)
    {
        var classes = await Classes.GetAllAsync(new ClassByInstructorSpec(instructorId));
        var courseIds = classes.Select(c => c.CourseId).Distinct().ToList();

        var courses = await Courses.GetAllAsync(new CourseSpec(courseIds));

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

        Courses.Add(course);
        await _unitOfWork.SaveChangesAsync();

        if (dto.PrerequisiteCodes is { Count: > 0 })
        {
            var allCourses = await Courses.GetAllAsync();
            var prereqCourses = allCourses
                .Where(c => dto.PrerequisiteCodes.Contains(c.CourseCode!))
                .ToList();

            foreach (var prereq in prereqCourses)
            {
                Prerequisites.Add(new CoursePrerequisite
                {
                    CourseId = course.CourseId,
                    PrerequisiteCourseId = prereq.CourseId
                });
            }

            await _unitOfWork.SaveChangesAsync();
        }

        var result = await Courses.GetByIdAsync(new CourseSpec(course.CourseId));
        return MapToDto(result!);
    }

    public async Task<bool> ActivateAsync(int courseId)
    {
        var course = await Courses.GetByIdAsync(courseId);
        if (course is null) return false;
        course.Status = CourseStatus.Active;
        Courses.Update(course);
        await _unitOfWork.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeactivateAsync(int courseId)
    {
        var course = await Courses.GetByIdAsync(courseId);
        if (course is null) return false;
        course.Status = CourseStatus.Inactive;
        Courses.Update(course);
        await _unitOfWork.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteAsync(int courseId)
    {
        var course = await Courses.GetByIdAsync(courseId);
        if (course is null) return false;
        Courses.Delete(course);
        await _unitOfWork.SaveChangesAsync();
        return true;
    }

    private async Task<int?> ResolveDepartmentIdAsync(string? departmentName)
    {
        if (string.IsNullOrWhiteSpace(departmentName))
            return null;

        if (int.TryParse(departmentName, out var id))
        {
            var deptNum = await Departments.GetByIdAsync(id);
            if (deptNum != null)
                return id;
        }

        var normalized = departmentName.Trim();
        var departments = await Departments.GetAllAsync();
        
        var department = departments
            .FirstOrDefault(d => string.Equals(d.DepartmentName, departmentName, StringComparison.OrdinalIgnoreCase));

        if (department is not null)
            return department.DepartmentId;

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
