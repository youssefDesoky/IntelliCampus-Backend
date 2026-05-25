using System.Text.Json.Serialization;

namespace IntelliCampus.shared.Dtos.Quiz;

public class QuizQuestionDto
{
    public string Id { get; set; } = string.Empty;

    public string Type { get; set; } = string.Empty;

    public string Prompt { get; set; } = string.Empty;

    public List<string>? Options { get; set; }

    public decimal Points { get; set; }

    public string? CorrectAnswer { get; set; }
}
