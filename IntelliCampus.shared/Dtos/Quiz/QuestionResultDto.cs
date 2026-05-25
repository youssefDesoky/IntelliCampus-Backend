using System.Text.Json.Serialization;

namespace IntelliCampus.shared.Dtos.Quiz;

public class QuestionResultDto
{
    public string QuestionId { get; set; } = string.Empty;

    public string Type { get; set; } = string.Empty;

    public decimal Points { get; set; }

    public decimal EarnedPoints { get; set; }

    public bool IsCorrect { get; set; }

    public string? Feedback { get; set; }
}
