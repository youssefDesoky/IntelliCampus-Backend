using System.Text.Json.Serialization;

namespace IntelliCampus.shared.Dtos.Quiz;

public class QuizSubmitResponseDto
{
    [JsonPropertyName("courseId")]
    public string CourseId { get; set; } = string.Empty;

    [JsonPropertyName("courseName")]
    public string CourseName { get; set; } = string.Empty;

    [JsonPropertyName("score")]
    public decimal Score { get; set; }

    [JsonPropertyName("maxScore")]
    public decimal MaxScore { get; set; }

    [JsonPropertyName("percentage")]
    public decimal Percentage { get; set; }

    [JsonPropertyName("answeredCount")]
    public int AnsweredCount { get; set; }

    [JsonPropertyName("byType")]
    public Dictionary<string, QuizTypeStatsDto> ByType { get; set; } = new();

    [JsonPropertyName("questionResults")]
    public List<QuestionResultDto> QuestionResults { get; set; } = new();

    [JsonPropertyName("answers")]
    public Dictionary<string, object> Answers { get; set; } = new();

    [JsonPropertyName("submittedAt")]
    public string SubmittedAt { get; set; } = string.Empty;
}
