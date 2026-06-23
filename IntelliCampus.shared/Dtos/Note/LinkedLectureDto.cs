using System.Text.Json.Serialization;

namespace IntelliCampus.Shared.Dtos.Note;

public class LinkedLectureDto
{
    public int Id { get; set; }
    public string Title { get; set; } = null!;
    public string ShortTitle { get; set; } = null!;
    public string WeekLabel { get; set; } = null!;
    public string? Description { get; set; }
    public int? CourseId { get; set; }

    [JsonPropertyName("materialFolderId")]
    public string MaterialFolderName { get; set; } = null!;
}
