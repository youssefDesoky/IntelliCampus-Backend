using IntelliCampus.BLL.Dtos.Registration;

namespace IntelliCampus.BLL.Services.Interfaces;

public interface IRegistrationService
{
    Task<StudentRegistrationDto?> RegisterStudentInCourseAsync(int studentId, CourseRegistrationDto dto);
    Task<IEnumerable<StudentRegistrationDto>> GetStudentRegistrationsAsync(int studentId);
    Task<bool> UnregisterStudentFromCourseAsync(int studentId, int courseId);
}
