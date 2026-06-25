using System.Text.Json.Serialization;

namespace IntelliCampus.shared.Dtos.Quiz;

public class QuizHistoryItemDto
{
    public string Id { get; set; } = string.Empty;

    public int DurationMinutes { get; set; }

    public string Title { get; set; } = string.Empty;

    public decimal? Score { get; set; }

    public decimal MaxScore { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime DueDate { get; set; }

    public string? Description { get; set; }

    public string Status { get; set; } = string.Empty;
}
