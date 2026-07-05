namespace IntelliCampus.Shared.Dtos.FaheemAi;

public class IndexCourseResponse
{
    public string Signal { get; set; } = null!;
    public string CourseCode { get; set; } = null!;
    public List<object> Indexed { get; set; } = [];
    public List<object> Skipped { get; set; } = [];
    public int TotalIndexedChunks { get; set; }
}
