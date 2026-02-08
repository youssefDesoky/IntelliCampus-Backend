using IntelliCampus.BLL.Dtos.Course;
using IntelliCampus.BLL.Services.Interfaces;
using IntelliCampus.DAL.Data.Contexts;
using IntelliCampus.DAL.Entities;
using IntelliCampus.DAL.Entities.Enums;
using Microsoft.EntityFrameworkCore;

namespace IntelliCampus.BLL.Services.Classes;

public class CourseService : ICourseService
{
    private readonly IntelliCampusDbContext _context;

    public CourseService(IntelliCampusDbContext context)
    {
        _context = context;
    }

    public async Task<CourseDto?> GetByIdAsync(int courseId)
    {
        var course = await _context.Courses
            .Include(c => c.Department)
            .Include(c => c.Classes)
            .Include(c => c.Prerequisites)
                .ThenInclude(p => p.PrerequisiteCourse)
            .FirstOrDefaultAsync(c => c.CourseId == courseId);

        if (course is null)
            return null;

        return MapToDto(course);
    }

    public async Task<IEnumerable<CourseDto>> GetAllAsync()
    {
        var courses = await _context.Courses
            .Include(c => c.Department)
            .Include(c => c.Classes)
            .Include(c => c.Prerequisites)
                .ThenInclude(p => p.PrerequisiteCourse)
            .ToListAsync();

        return courses.Select(MapToDto);
    }

    public async Task<IEnumerable<CourseDto>> GetActiveCoursesAsync()
    {
        var courses = await _context.Courses
            .Include(c => c.Department)
            .Include(c => c.Classes)
            .Include(c => c.Prerequisites)
                .ThenInclude(p => p.PrerequisiteCourse)
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

        var courses = await _context.Courses
            .Include(c => c.Department)
            .Include(c => c.Classes)
            .Include(c => c.Prerequisites)
                .ThenInclude(p => p.PrerequisiteCourse)
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

        var courses = await _context.Courses
            .Include(c => c.Department)
            .Include(c => c.Classes)
            .Include(c => c.Prerequisites)
                .ThenInclude(p => p.PrerequisiteCourse)
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
            Description = dto.Description,
            CreditHours = dto.CreditHours,
            Status = CourseStatus.Active,
            DepartmentId = departmentId
        };

        _context.Courses.Add(course);
        await _context.SaveChangesAsync();

        // Resolve prerequisite codes to course IDs
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

        // Reload with relations
        if (course.DepartmentId.HasValue)
            await _context.Entry(course).Reference(c => c.Department).LoadAsync();
        await _context.Entry(course).Collection(c => c.Prerequisites).LoadAsync();

        return MapToDto(course);
    }

    public async Task<bool> ActivateAsync(int courseId)
    {
        var course = await _context.Courses.FindAsync(courseId);

        if (course is null)
            return false;

        course.Status = CourseStatus.Active;
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<bool> DeactivateAsync(int courseId)
    {
        var course = await _context.Courses.FindAsync(courseId);

        if (course is null)
            return false;

        course.Status = CourseStatus.Inactive;
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<bool> DeleteAsync(int courseId)
    {
        var course = await _context.Courses.FindAsync(courseId);

        if (course is null)
            return false;

        _context.Courses.Remove(course);
        await _context.SaveChangesAsync();

        return true;
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
        return new CourseDto
        {
            CourseId = course.CourseId,
            CourseCode = course.CourseCode,
            CourseName = course.CourseName,
            CourseNameAr = course.CourseNameAr,
            Description = course.Description,
            CreditHours = course.CreditHours,
            Status = course.Status,
            DepartmentId = course.DepartmentId,
            DepartmentName = course.Department?.DepartmentName,
            ClassCount = course.Classes?.Count ?? 0,
            Prerequisites = course.Prerequisites?
                .Select(p => p.PrerequisiteCourse?.CourseCode ?? p.PrerequisiteCourseId.ToString())
                .ToList()
        };
    }
}
