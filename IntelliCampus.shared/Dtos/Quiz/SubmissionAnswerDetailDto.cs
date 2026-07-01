namespace IntelliCampus.shared.Dtos.Quiz;

public class SubmissionAnswerDetailDto
{
    public string QuestionId { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Prompt { get; set; } = string.Empty;
    public List<string>? Options { get; set; }
    public decimal Points { get; set; }
    public string? StudentAnswer { get; set; }
    public string? CorrectAnswer { get; set; }
    public decimal? AutoScore { get; set; }
    public decimal? Score { get; set; }
}
