using System.Text.Json.Serialization;

namespace IntelliCampus.shared.Dtos.Quiz;

public class PracticeQuizDto
{
    [JsonPropertyName("courseId")]
    public string CourseId { get; set; } = string.Empty;

    [JsonPropertyName("quizId")]
    public int QuizId { get; set; }

    [JsonPropertyName("courseName")]
    public string CourseName { get; set; } = string.Empty;

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("durationSeconds")]
    public int DurationSeconds { get; set; }

    [JsonPropertyName("pageSize")]
    public int PageSize { get; set; }

    [JsonPropertyName("maxAttempts")]
    public int MaxAttempts { get; set; }

    [JsonPropertyName("questions")]
    public List<QuizQuestionDto> Questions { get; set; } = new();

    [JsonPropertyName("questionsSummary")]
    public QuizQuestionsSummaryDto QuestionsSummary { get; set; } = new();

    [JsonPropertyName("previousSubmission")]
    public object? PreviousSubmission { get; set; }

    [JsonPropertyName("isSubmitted")]
    public bool IsSubmitted { get; set; }
}
