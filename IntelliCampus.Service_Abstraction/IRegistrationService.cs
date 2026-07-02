using IntelliCampus.Shared.Dtos.Registration;

namespace IntelliCampus.Service_Abstraction;

public interface IRegistrationService
{
    Task<StudentRegistrationDto?> RegisterStudentInCourseAsync(int studentId, CourseRegistrationDto dto);
    Task<IEnumerable<StudentRegistrationDto>> GetStudentRegistrationsAsync(int studentId);
    Task<bool> UnregisterStudentFromCourseAsync(int studentId, int courseId);
    Task ChangeStudentCourseSectionAsync(int studentId, int courseId, int newClassId);
    Task UnlinkClassFromRegistrationAsync(int studentId, int courseId);
    Task<RegistrationSettingsDto> GetRegistrationSettingsAsync(int studentId);
}
