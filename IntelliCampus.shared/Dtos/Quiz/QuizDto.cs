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
