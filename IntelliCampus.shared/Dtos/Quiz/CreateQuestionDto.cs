namespace IntelliCampus.shared.Dtos.Quiz;

public class CreateQuestionDto
{
    public string Type { get; set; } = string.Empty;
    public string Prompt { get; set; } = string.Empty;
    public List<string>? Options { get; set; }
    public decimal Points { get; set; }
    public string? CorrectAnswer { get; set; }
}
