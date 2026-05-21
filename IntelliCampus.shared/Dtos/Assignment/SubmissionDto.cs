namespace IntelliCampus.Shared.Dtos.Assignment;

public class SubmissionDto
{
    public string Id { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime SubmittedAt { get; set; }
    public bool IsLate { get; set; }
    public string? Note { get; set; }
    public List<SubmissionFileDto> Files { get; set; } = [];
}
