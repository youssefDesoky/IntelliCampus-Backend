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
        var course = await _context.Courses.FindAsync(courseId);

        if (course is null)
            return null;

        return MapToDto(course);
    }

    public async Task<IEnumerable<CourseDto>> GetAllAsync()
    {
        var courses = await _context.Courses.ToListAsync();
        return courses.Select(MapToDto);
    }

    public async Task<IEnumerable<CourseDto>> GetActiveCoursesAsync()
    {
        var courses = await _context.Courses
            .Where(c => c.Status == CourseStatus.Active)
            .ToListAsync();

        return courses.Select(MapToDto);
    }

    public async Task<CourseDto> CreateAsync(CreateCourseDto dto)
    {
        var course = new Course
        {
            CourseName = dto.CourseName,
            CourseNameAr = dto.CourseNameAr,
            CreditHours = dto.CreditHours,
            Status = CourseStatus.Active
        };

        _context.Courses.Add(course);
        await _context.SaveChangesAsync();

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

    private static CourseDto MapToDto(Course course)
    {
        return new CourseDto
        {
            CourseId = course.CourseId,
            CourseName = course.CourseName,
            CourseNameAr = course.CourseNameAr,
            CreditHours = course.CreditHours,
            Status = course.Status
        };
    }
}
