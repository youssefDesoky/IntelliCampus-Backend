namespace IntelliCampus.Shared.Dtos.FaheemAi;

public class UploadMaterialResponse
{
    public string Signal { get; set; } = null!;
    public int FileId { get; set; }
    public string FileType { get; set; } = null!;
    public string? LectureId { get; set; }
    public string? LectureName { get; set; }
    public string CourseCode { get; set; } = null!;
}
