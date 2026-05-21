using System.Text.Json.Serialization;

namespace IntelliCampus.shared.Dtos.Quiz;

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
