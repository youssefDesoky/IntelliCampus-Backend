using IntelliCampus.Shared.Dtos.Grade;

namespace IntelliCampus.Service_Abstraction;

public interface IGradeService
{
    // Student
    Task<CourseGradeDto?> GetCourseGradeAsync(int studentId, int courseId);
    Task<int> GetCourseWorkAsync(int studentId, int courseId);
    Task<IEnumerable<GradeHistoryItemDto>> GetAllGradesAsync(int studentId);
    Task<IEnumerable<TranscriptCourseDto>> GetTranscriptAsync(int studentId);
    Task<byte[]> ExportTranscriptPdfAsync(int studentId);

    // Instructor (read-only)
    Task<IEnumerable<GradeDto>> GetByStudentAndCourseAsync(int instructorId, int studentId, int courseId);

    // Complaints
    Task<GradeComplaintResponseDto> FileComplaintAsync(int studentId, GradeComplaintDto dto);
    Task<IEnumerable<GradeComplaintResponseDto>> GetComplaintsAsync(int studentId);
    Task<GradeComplaintResponseDto?> ReviewComplaintAsync(int complaintId, int instructorId);
}
