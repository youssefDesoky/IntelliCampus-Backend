using System.Text.Json.Serialization;

namespace IntelliCampus.shared.Dtos.Quiz;

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
    public string SubmittedAt { get; set; } = string.Empty;

    [JsonPropertyName("answers")]
    public Dictionary<string, object>? Answers { get; set; }

    [JsonPropertyName("questionResults")]
    public List<QuestionResultDto>? QuestionResults { get; set; }
}
