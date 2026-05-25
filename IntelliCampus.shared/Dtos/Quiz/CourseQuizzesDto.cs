using System.Text.Json.Serialization;

namespace IntelliCampus.shared.Dtos.Quiz;

public class CourseQuizzesDto
{
    public string CourseId { get; set; } = string.Empty;

    public string CourseName { get; set; } = string.Empty;

    public List<QuizHistoryItemDto> History { get; set; } = new();

    public List<QuizUpcomingItemDto> Upcoming { get; set; } = new();

    public QuizStatsDto Stats { get; set; } = new();
}
