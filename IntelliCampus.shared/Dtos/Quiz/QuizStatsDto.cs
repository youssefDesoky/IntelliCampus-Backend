using System.Text.Json.Serialization;

namespace IntelliCampus.shared.Dtos.Quiz;

public class QuizStatsDto
{
    [JsonPropertyName("completed")]
    public int Completed { get; set; }

    [JsonPropertyName("missed")]
    public int Missed { get; set; }

    [JsonPropertyName("upcoming")]
    public int Upcoming { get; set; }

    [JsonPropertyName("averageScore")]
    public decimal AverageScore { get; set; }
}
