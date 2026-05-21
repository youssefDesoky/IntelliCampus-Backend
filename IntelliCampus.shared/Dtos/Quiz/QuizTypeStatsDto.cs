using System.Text.Json.Serialization;

namespace IntelliCampus.shared.Dtos.Quiz;

public class QuizTypeStatsDto
{
    [JsonPropertyName("answered")]
    public int Answered { get; set; }

    [JsonPropertyName("total")]
    public int Total { get; set; }

    [JsonPropertyName("score")]
    public decimal Score { get; set; }
}
