using IntelliCampus.shared.Pagination;
using IntelliCampus.Shared.Dtos.Grade;
using IntelliCampus.Shared.Params;

namespace IntelliCampus.Service_Abstraction;

public interface IGradeService
{
    // Student
    Task<CourseGradeDto?> GetCourseGradeAsync(int studentId, int courseId);
    Task<PaginatedResult<CourseGradeDto>> GetCourseGradeAsync(int studentId, int courseId, GradeQueryParams queryParams);
    Task<int> GetCourseWorkAsync(int studentId, int courseId);
    Task<IEnumerable<GradeHistoryItemDto>> GetAllGradesAsync(int studentId);
    Task<IEnumerable<TranscriptCourseDto>> GetTranscriptAsync(int studentId);
    Task<double> GetCumulativeGpaAsync(int studentId);
    Task<byte[]> ExportTranscriptPdfAsync(int studentId);

    /// <summary>
    /// Checks if all courses for the student have complete grades (total + letter).
    /// If so, recalculates cumulative GPA and persists it to Student.Gpa.
    /// Returns the new GPA, or null if not all courses are complete.
    /// </summary>
    Task<double?> UpdateStudentGpaIfCompleteAsync(int studentId);

    /// <summary>
    /// Returns the total completed credit hours for a student
    /// (sum of CreditHours for courses where overall letter grade is valid and not "-").
    /// </summary>
    Task<int> GetCompletedHoursAsync(int studentId);

    /// <summary>
    /// Checks the student's Bylaw LevelScales and promotes the student to the
    /// highest level whose MinHours is <= the student's completed hours.
    /// Returns the new level, or null if unchanged.
    /// </summary>
    Task<int?> UpdateStudentLevelIfPromotedAsync(int studentId);

    // Instructor (read-only)
    Task<IEnumerable<GradeDto>> GetByStudentAndCourseAsync(int instructorId, int studentId, int courseId);

    // Complaints
    Task<GradeComplaintResponseDto> FileComplaintAsync(int studentId, GradeComplaintDto dto);
    Task<IEnumerable<GradeComplaintResponseDto>> GetComplaintsAsync(int studentId);
    Task<GradeComplaintResponseDto?> ReviewComplaintAsync(int complaintId, int instructorId);
}
