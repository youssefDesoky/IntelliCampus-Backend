namespace IntelliCampus.Shared.Dtos.FaheemAi;

public class AdvisorQuestionRequest
{
    public string Question { get; set; } = null!;
    public string? StudentCode { get; set; }
    public string? Department { get; set; }
}
