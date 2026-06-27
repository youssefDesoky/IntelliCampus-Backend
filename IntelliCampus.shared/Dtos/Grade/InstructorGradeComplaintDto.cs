namespace IntelliCampus.Shared.Dtos.Grade;

public class InstructorGradeComplaintDto
{
    public int Id { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public string ComplaintType { get; set; } = string.Empty;
    public string AssessmentTitle { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string CreatedAt { get; set; } = string.Empty;
    public string? InstructorResponse { get; set; }
}
