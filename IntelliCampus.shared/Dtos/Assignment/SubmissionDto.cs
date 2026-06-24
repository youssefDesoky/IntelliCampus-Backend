namespace IntelliCampus.Shared.Dtos.Assignment;

public class SubmissionDto
{
    public string Id { get; set; } = string.Empty;
    public int StudentId { get; set; }
    public string? StudentName { get; set; }
    public string Status { get; set; } = string.Empty;
    public string SubmittedAt { get; set; } = string.Empty;
    public bool IsLate { get; set; }
    public string? Note { get; set; }
    public List<SubmissionFileDto> Files { get; set; } = [];
    public GradeInfoDto? Grade { get; set; }
}