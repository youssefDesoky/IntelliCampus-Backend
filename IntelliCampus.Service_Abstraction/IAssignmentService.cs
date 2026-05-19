using IntelliCampus.Shared.Dtos.Assignment;

namespace IntelliCampus.Service_Abstraction;

public interface IAssignmentService
{
    // Instructor
    Task<AssignmentDto?> GetByIdAsync(int assignmentId);
    Task<IEnumerable<AssignmentDto>> GetByClassIdAsync(int classId);
    Task<AssignmentDto> CreateAsync(int instructorId, CreateAssignmentDto dto);
    Task<bool> DeleteAsync(int assignmentId, int instructorId);

    // Submissions
    Task<SubmissionDto> SubmitAsync(int studentId, SubmitAssignmentDto dto);
    Task<SubmissionDto?> GetSubmissionAsync(int studentId, int assignmentId);
    Task<IEnumerable<SubmissionDto>> GetAllSubmissionsAsync(int assignmentId, int instructorId);
    Task<SubmissionDto?> GradeSubmissionAsync(int instructorId, GradeSubmissionDto dto);

    // Student
    Task<IEnumerable<SubmissionDto>> GetByStudentIdAsync(int studentId);
}
