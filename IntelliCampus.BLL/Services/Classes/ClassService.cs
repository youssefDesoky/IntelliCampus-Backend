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
        // Validate course exists
        var courseExists = await _context.Courses.AnyAsync(c => c.CourseId == dto.CourseId);
        if (!courseExists)
            throw new InvalidOperationException("Course not found.");

        // Only one Lecture class per course
        if (dto.ClassType == ClassType.Lecture)
        {
            var lectureExists = await _context.Classes
                .AnyAsync(c => c.CourseId == dto.CourseId && c.ClassType == ClassType.Lecture);

            if (lectureExists)
                throw new InvalidOperationException("A lecture class already exists for this course. Only one lecture is allowed per course.");
        }

        // Validate instructor role matches class type
        if (dto.InstructorId.HasValue)
        {
            var instructor = await _context.Instructors.FirstOrDefaultAsync(i => i.UserId == dto.InstructorId);
            if (instructor is null)
                throw new InvalidOperationException("Instructor not found.");

            ValidateInstructorRoleForClassType(instructor, dto.ClassType);
        }

        var classEntity = new Class
        {
            ClassType = dto.ClassType,
            CourseId = dto.CourseId,
            InstructorId = dto.InstructorId
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

        // Validate instructor role matches class type
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

    private static ClassDto MapToDto(Class classEntity)
    {
        return new ClassDto
        {
            ClassId = classEntity.ClassId,
            ClassType = classEntity.ClassType,
            CourseId = classEntity.CourseId,
            CourseName = classEntity.Course.CourseName,
            InstructorId = classEntity.InstructorId,
            InstructorName = classEntity.Instructor?.FullName
        };
    }
}
