using System.Text.Json.Serialization;

namespace IntelliCampus.shared.Dtos.Quiz;

public class QuizUpcomingItemDto
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("durationMinutes")]
    public int DurationMinutes { get; set; }

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("maxScore")]
    public decimal MaxScore { get; set; }

    [JsonPropertyName("deadline")]
    public DateTime Deadline { get; set; }

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;
}
