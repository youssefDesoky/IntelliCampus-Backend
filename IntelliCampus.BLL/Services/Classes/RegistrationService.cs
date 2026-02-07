using IntelliCampus.BLL.Dtos.Registration;
using IntelliCampus.BLL.Services.Interfaces;
using IntelliCampus.BLL.Utilities;
using IntelliCampus.DAL.Data.Contexts;
using IntelliCampus.DAL.Entities;
using Microsoft.EntityFrameworkCore;

namespace IntelliCampus.BLL.Services.Classes;

public class RegistrationService : IRegistrationService
{
    private readonly IntelliCampusDbContext _context;

    public RegistrationService(IntelliCampusDbContext context)
    {
        _context = context;
    }

    public async Task<StudentRegistrationDto?> RegisterStudentInCourseAsync(int studentId, CourseRegistrationDto dto)
    {
        // Verify student exists (UserId is the PK in TPT inheritance)
        var studentExists = await _context.Students.AnyAsync(s => s.UserId == studentId);
        if (!studentExists)
            throw new InvalidOperationException("Student not found.");

        // Verify course exists
        var course = await _context.Courses.FindAsync(dto.CourseId);
        if (course is null)
            throw new InvalidOperationException("Course not found.");

        // Verify class exists and belongs to the course
        var classEntity = await _context.Classes
            .FirstOrDefaultAsync(c => c.ClassId == dto.ClassId && c.CourseId == dto.CourseId);
        if (classEntity is null)
            throw new InvalidOperationException("Class not found or does not belong to the specified course.");

        // Check if already registered
        var existingRegistration = await _context.StudentCourses
            .FirstOrDefaultAsync(sc => sc.StudentId == studentId && sc.CourseId == dto.CourseId);
        if (existingRegistration is not null)
            throw new InvalidOperationException("Student is already registered in this course.");

        // Auto-generate semester based on current date
        var semester = SemesterHelper.GetCurrentSemester();

        var studentCourse = new StudentCourse
        {
            StudentId = studentId,
            CourseId = dto.CourseId,
            ClassId = dto.ClassId,
            Semester = semester,
            RegisteredAt = DateTime.UtcNow
        };

        _context.StudentCourses.Add(studentCourse);
        await _context.SaveChangesAsync();

        return new StudentRegistrationDto
        {
            StudentId = studentId,
            CourseId = dto.CourseId,
            CourseName = course.CourseName,
            ClassId = dto.ClassId,
            ClassName = $"{classEntity.ClassType}",
            Semester = semester,
            RegisteredAt = studentCourse.RegisteredAt
        };
    }

    public async Task<IEnumerable<StudentRegistrationDto>> GetStudentRegistrationsAsync(int studentId)
    {
        var registrations = await _context.StudentCourses
            .Include(sc => sc.Course)
            .Include(sc => sc.Class)
            .Where(sc => sc.StudentId == studentId)
            .ToListAsync();

        return registrations.Select(sc => new StudentRegistrationDto
        {
            StudentId = sc.StudentId,
            CourseId = sc.CourseId,
            CourseName = sc.Course.CourseName,
            ClassId = sc.ClassId,
            ClassName = sc.Class is not null ? $"{sc.Class.ClassType}" : null,
            Semester = sc.Semester,
            RegisteredAt = sc.RegisteredAt
        });
    }

    public async Task<bool> UnregisterStudentFromCourseAsync(int studentId, int courseId)
    {
        var registration = await _context.StudentCourses
            .FirstOrDefaultAsync(sc => sc.StudentId == studentId && sc.CourseId == courseId);

        if (registration is null)
            return false;

        _context.StudentCourses.Remove(registration);
        await _context.SaveChangesAsync();

        return true;
    }
}
