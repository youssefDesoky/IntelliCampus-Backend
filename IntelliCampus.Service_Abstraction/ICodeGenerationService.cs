using IntelliCampus.Domain.Entities.Enums;

namespace IntelliCampus.Service_Abstraction;

public interface ICodeGenerationService
{
    Task<string> GenerateStudentCodeAsync(int facultyId, DateTime date, StudentType studentType);
    Task<string> GenerateInstructorCodeAsync(int facultyId, DateTime date);
    Task<string> GenerateAdminCodeAsync(int facultyId, DateTime date);
}
