namespace IntelliCampus.Shared.Dtos.Grade;

public class GradeComplaintDto
{
    public int GradeId { get; set; }
    public string ComplaintType { get; set; } = string.Empty;
    public string Details { get; set; } = string.Empty;
}
