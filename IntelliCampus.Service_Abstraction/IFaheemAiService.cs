namespace IntelliCampus.Service_Abstraction;

public interface IFaheemAiService
{
    Task<string> EnhanceNoteAsync(string courseCode, string notes, string? lectureId = null, CancellationToken ct = default);
    Task<string> AskAdvisorAsync(string question, string? studentCode = null, string? department = null, CancellationToken ct = default);
    Task<UploadMaterialResult> UploadCourseMaterialAsync(string courseCode, string filePath, string fileName, string type = "other", string? lectureId = null, string? lectureName = null, CancellationToken ct = default);
    Task<int> ProcessCourseMaterialAsync(string courseCode, int? fileId = null, CancellationToken ct = default);
    Task<int> IndexCourseMaterialAsync(string courseCode, int? fileId = null, CancellationToken ct = default);
    Task<string> AskCourseAsync(string courseCode, string question, string? studentCode = null, CancellationToken ct = default);
    Task<string> AskCourseAsync(string courseCode, string question, string? studentCode = null, Stream? attachmentStream = null, string? attachmentFileName = null, CancellationToken ct = default);
}

public record UploadMaterialResult(int FileId, string FileType, string? LectureId, string? LectureName, string CourseCode);
