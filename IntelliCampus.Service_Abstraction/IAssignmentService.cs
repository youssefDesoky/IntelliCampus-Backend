using IntelliCampus.Shared.Dtos.Assignment;

namespace IntelliCampus.Service_Abstraction;

public interface IAssignmentService
{
    // Instructor
    Task<AssignmentDto?> GetByIdAsync(int assignmentId, int? studentId = null);
    Task<IEnumerable<AssignmentDto>> GetByCourseIdAsync(int courseId, int? studentId = null);
    Task<AssignmentDto> CreateAsync(int instructorId, CreateAssignmentDto dto);
    Task<bool> DeleteAsync(int assignmentId, int instructorId);
    Task<IEnumerable<SubmissionDto>> GetAllSubmissionsAsync(int assignmentId, int instructorId);
    Task<AssignmentDto?> GradeSubmissionAsync(int instructorId, GradeSubmissionDto dto);
    Task<AssignmentStatsDto> GetStatsAsync(int courseId, int studentId);

    // Student
    Task<SubmissionDto> SubmitAsync(int studentId, int assignmentId, SubmitAssignmentDto dto);
    Task<IEnumerable<AssignmentDto>> GetByStudentAndCourseAsync(int studentId, int courseId);
}
