using System.Text.Json.Serialization;

namespace IntelliCampus.Shared.Dtos.Class;

public class CreateClassDto
{
    public string Type { get; set; } = null!;

    public string? InstructorName { get; set; }

    public string? Schedule { get; set; }

    public string? Room { get; set; }
    public int CourseId { get; set; }
}
