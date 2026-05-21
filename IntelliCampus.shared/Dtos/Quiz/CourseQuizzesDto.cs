using System.Text.Json.Serialization;

namespace IntelliCampus.shared.Dtos.Quiz;

public class CourseQuizzesDto
{
    [JsonPropertyName("courseId")]
    public string CourseId { get; set; } = string.Empty;

    [JsonPropertyName("courseName")]
    public string CourseName { get; set; } = string.Empty;

    [JsonPropertyName("history")]
    public List<QuizHistoryItemDto> History { get; set; } = new();

    [JsonPropertyName("upcoming")]
    public List<QuizUpcomingItemDto> Upcoming { get; set; } = new();

    [JsonPropertyName("stats")]
    public QuizStatsDto Stats { get; set; } = new();
}
