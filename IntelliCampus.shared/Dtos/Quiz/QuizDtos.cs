using System.Text.Json.Serialization;

namespace IntelliCampus.shared.Dtos.Quiz;

public class QuizDto
{
    [JsonPropertyName("id")]
    public int Id { get; set; }
    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;
    [JsonPropertyName("description")]
    public string? Description { get; set; }
    [JsonPropertyName("deadline")]
    public DateTime Deadline { get; set; }
    [JsonPropertyName("durationMinutes")]
    public int DurationMinutes { get; set; }
    [JsonPropertyName("maxScore")]
    public decimal MaxScore { get; set; }
    [JsonPropertyName("classId")]
    public int ClassId { get; set; }
    [JsonPropertyName("className")]
    public string? ClassName { get; set; }
    [JsonPropertyName("courseName")]
    public string? CourseName { get; set; }
    [JsonPropertyName("status")]
    public string Status { get; set; } = "Upcoming";
}

public class CreateQuizDto
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime DueDate { get; set; }
    public int DurationMinutes { get; set; }
    public decimal MaxGrade { get; set; }
    public int ClassId { get; set; }
}

public class SubmitQuizDto
{
    public int QuizId { get; set; }
    public Dictionary<string, object> Answers { get; set; } = new();
}

public class StudentQuizDto
{
    public int StudentQuizId { get; set; }
    public int StudentId { get; set; }
    public string? StudentName { get; set; }
    public int QuizId { get; set; }
    public string? QuizTitle { get; set; }
    public decimal Score { get; set; }
    public decimal MaxGrade { get; set; }
    public DateTime SubmittedAt { get; set; }
    public bool IsLate { get; set; }
}

// ----------------------------------------------------
// DTOs matching the exact JSON layouts from screenshots
// ----------------------------------------------------

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
    public DateTime SubmittedAt { get; set; }
}

public class QuizTypeStatsDto
{
    [JsonPropertyName("answered")]
    public int Answered { get; set; }

    [JsonPropertyName("total")]
    public int Total { get; set; }

    [JsonPropertyName("score")]
    public decimal Score { get; set; }
}

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

public class PracticeQuizDto
{
    [JsonPropertyName("courseId")]
    public string CourseId { get; set; } = string.Empty;

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

public class QuizQuestionDto
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("prompt")]
    public string Prompt { get; set; } = string.Empty;

    [JsonPropertyName("options")]
    public List<string>? Options { get; set; }

    [JsonPropertyName("points")]
    public decimal Points { get; set; }

    [JsonPropertyName("correctAnswer")]
    public string? CorrectAnswer { get; set; }
}

public class QuizQuestionsSummaryDto
{
    [JsonPropertyName("total")]
    public int Total { get; set; }

    [JsonPropertyName("tf")]
    public int Tf { get; set; }

    [JsonPropertyName("mcq")]
    public int Mcq { get; set; }

    [JsonPropertyName("written")]
    public int Written { get; set; }
}

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

public class QuizHistoryItemDto
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("score")]
    public decimal Score { get; set; }

    [JsonPropertyName("maxScore")]
    public decimal MaxScore { get; set; }

    [JsonPropertyName("deadline")]
    public DateTime Deadline { get; set; }

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;
}

public class QuizUpcomingItemDto
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("maxScore")]
    public decimal MaxScore { get; set; }

    [JsonPropertyName("deadline")]
    public DateTime Deadline { get; set; }

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;
}

public class CreateQuestionDto
{
    public string Type { get; set; } = string.Empty;
    public string Prompt { get; set; } = string.Empty;
    public List<string>? Options { get; set; }
    public decimal Points { get; set; }
    public string? CorrectAnswer { get; set; }
}

public class StudentSubmissionDto
{
    [JsonPropertyName("studentId")]
    public int StudentId { get; set; }

    [JsonPropertyName("studentName")]
    public string? StudentName { get; set; }

    [JsonPropertyName("score")]
    public decimal Score { get; set; }

    [JsonPropertyName("maxScore")]
    public decimal MaxScore { get; set; }

    [JsonPropertyName("submittedAt")]
    public DateTime SubmittedAt { get; set; }

    [JsonPropertyName("answers")]
    public Dictionary<string, object>? Answers { get; set; }

    [JsonPropertyName("questionResults")]
    public List<QuestionResultDto>? QuestionResults { get; set; }
}

public class GradeWrittenDto
{
    [JsonPropertyName("questionScores")]
    public Dictionary<string, decimal> QuestionScores { get; set; } = new();
}

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
