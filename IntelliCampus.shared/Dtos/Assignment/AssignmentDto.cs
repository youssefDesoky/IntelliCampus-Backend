namespace IntelliCampus.Shared.Dtos.Assignment;

public class AssignmentDto
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? FullInstructions { get; set; }
    public DateTime DueDate { get; set; }
    public decimal TotalPoints { get; set; }
    public List<AssignmentAttachmentDto> Attachments { get; set; } = [];

    // Student-specific fields (null when fetched by instructor)
    public string? Status { get; set; }
    public SubmissionDto? Submission { get; set; }
    public GradeInfoDto? Grade { get; set; }
}
