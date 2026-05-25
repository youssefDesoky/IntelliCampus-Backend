using System.Text.Json.Serialization;

namespace IntelliCampus.shared.Dtos.Quiz;

public class GradeWrittenDto
{
    public Dictionary<string, decimal> QuestionScores { get; set; } = new();
}
