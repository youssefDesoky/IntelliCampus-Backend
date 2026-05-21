namespace IntelliCampus.Shared.Dtos.Grade;

public class GradeComplaintResponseDto
{
    public int ComplaintId { get; set; }
    public int GradeId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string ComplaintType { get; set; } = string.Empty;
    public string Details { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string SubmittedAt { get; set; } = string.Empty;
}
