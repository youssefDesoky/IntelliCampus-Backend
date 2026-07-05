namespace IntelliCampus.Shared.Dtos.FaheemAi;

public class CourseQuestionRequest
{
    public string Question { get; set; } = null!;
    public string CourseCode { get; set; } = null!;
    public string? StudentCode { get; set; }
}
