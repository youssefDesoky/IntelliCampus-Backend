using System.Text.Json.Serialization;

namespace IntelliCampus.shared.Dtos.Quiz;

public class QuizTypeStatsDto
{
    public int Answered { get; set; }

    public int Total { get; set; }

    public decimal Score { get; set; }
}
