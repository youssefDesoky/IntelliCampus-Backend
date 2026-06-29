using IntelliCampus.shared.Pagination;
using IntelliCampus.Shared.Dtos.Assignment;
using IntelliCampus.Shared.Params;
using Microsoft.AspNetCore.Http;

namespace IntelliCampus.Service_Abstraction;

public interface IAssignmentService
{
    // Instructor
    Task<AssignmentDto> GetByIdAsync(int assignmentId, int? studentId = null);
    Task<IEnumerable<AssignmentDto>> GetByCourseIdAsync(int courseId, int? studentId = null);
    Task<AssignmentDto> CreateAsync(int instructorId, CreateAssignmentDto dto);
    Task<AssignmentDto> UpdateAsync(int instructorId, int assignmentId, UpdateAssignmentDto dto);
    Task DeleteAsync(int assignmentId, int instructorId);
    Task<IEnumerable<SubmissionDto>> GetAllSubmissionsAsync(int assignmentId, int instructorId);
    Task<AssignmentDto> GradeSubmissionAsync(int instructorId, GradeSubmissionDto dto);
    Task<AssignmentStatsDto> GetStatsAsync(int courseId, int studentId);
    Task<(Stream Stream, string FileName, string ContentType)> DownloadSubmissionFileAsync(string fileId);
    Task<(Stream Stream, string FileName, string ContentType)> DownloadAssignmentAttachmentAsync(string fileId);
    Task<AssignmentAttachmentDto> UploadAttachmentAsync(IFormFile file);

    // Student
    Task<SubmissionDto> SubmitAsync(int studentId, int assignmentId, SubmitAssignmentDto dto, IFormFileCollection? files);
    Task<IEnumerable<AssignmentDto>> GetByStudentAndCourseAsync(int studentId, int courseId);
    Task<PaginatedResult<AssignmentDto>> GetByStudentAndCourseAsync(int studentId, int courseId, AssignmentQueryParams queryParams);
}