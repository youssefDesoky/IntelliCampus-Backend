using System.Text.Json.Serialization;

namespace IntelliCampus.shared.Dtos.Quiz;

public class QuizSubmitResponseDto
{
    public string CourseId { get; set; } = string.Empty;

    public string CourseName { get; set; } = string.Empty;

    public decimal? Score { get; set; }

    public decimal MaxScore { get; set; }

    public decimal Percentage { get; set; }

    public int AnsweredCount { get; set; }

    public Dictionary<string, QuizTypeStatsDto> ByType { get; set; } = new();

    public List<QuestionResultDto> QuestionResults { get; set; } = new();

    public Dictionary<string, object> Answers { get; set; } = new();

    public string SubmittedAt { get; set; } = string.Empty;
}
