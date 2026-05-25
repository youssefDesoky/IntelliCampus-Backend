using System.Text.Json.Serialization;

namespace IntelliCampus.shared.Dtos.Quiz;

public class PracticeQuizDto
{
    public string CourseId { get; set; } = string.Empty;

    public int QuizId { get; set; }

    public string CourseName { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public int DurationSeconds { get; set; }

    public int PageSize { get; set; }

    public int MaxAttempts { get; set; }

    public List<QuizQuestionDto> Questions { get; set; } = new();

    public QuizQuestionsSummaryDto QuestionsSummary { get; set; } = new();

    public object? PreviousSubmission { get; set; }

    public bool IsSubmitted { get; set; }
}
