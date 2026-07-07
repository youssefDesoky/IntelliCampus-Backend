using IntelliCampus.Domain.Entities.Enums;

namespace IntelliCampus.Service_Abstraction;

public interface ICurrentAdminContext
{
    int? UserId { get; }
    bool IsSuperAdmin { get; }
    bool IsAcademicStaff { get; }
    bool IsAdmin { get; }
    StudentType? AdminStudentType { get; }

    Task<int?> GetFacultyIdAsync();
    Task EnsureCanAccessFacultyAsync(int? resourceFacultyId);
    Task EnsureCanAccessByUserFacultyAsync(int userId);
    Task EnsureCanAccessCourseAsync(int courseId);
    Task EnsureCanAccessExamAsync(int examId);
    Task EnsureAdminHasFacultyAsync();
}
