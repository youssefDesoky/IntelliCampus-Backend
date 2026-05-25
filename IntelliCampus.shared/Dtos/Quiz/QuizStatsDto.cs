using System.Text.Json.Serialization;

namespace IntelliCampus.shared.Dtos.Quiz;

public class QuizStatsDto
{
    public int Completed { get; set; }

    public int Missed { get; set; }

    public int Upcoming { get; set; }

    public decimal AverageScore { get; set; }
}
