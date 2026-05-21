using System.Text.Json.Serialization;

namespace IntelliCampus.shared.Dtos.Quiz;

public class QuestionResultDto
{
    [JsonPropertyName("questionId")]
    public string QuestionId { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("points")]
    public decimal Points { get; set; }

    [JsonPropertyName("earnedPoints")]
    public decimal EarnedPoints { get; set; }

    [JsonPropertyName("isCorrect")]
    public bool IsCorrect { get; set; }

    [JsonPropertyName("feedback")]
    public string? Feedback { get; set; }
}
