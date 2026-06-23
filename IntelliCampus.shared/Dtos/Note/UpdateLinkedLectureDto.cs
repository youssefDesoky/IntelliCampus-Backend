using System.Text.Json.Serialization;

namespace IntelliCampus.Shared.Dtos.Note;

public class UpdateLinkedLectureDto
{
    public int? MaterialFolderId { get; set; }
    public string? Name { get; set; }
    public string? Title { get; set; }
    public string? ShortTitle { get; set; }
    public string? WeekLabel { get; set; }
    public string? Description { get; set; }
    public int? CourseId { get; set; }
}
