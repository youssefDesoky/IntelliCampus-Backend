using System.Text.Json.Serialization;

namespace IntelliCampus.Shared.Dtos.Class;

public class CreateClassDto
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = null!;

    [JsonPropertyName("instructor")]
    public string? InstructorName { get; set; }

    [JsonPropertyName("schedule")]
    public string? Schedule { get; set; }

    public string? Room { get; set; }
    public int CourseId { get; set; }
}
