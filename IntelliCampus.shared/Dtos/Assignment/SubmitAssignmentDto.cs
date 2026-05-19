namespace IntelliCampus.Shared.Dtos.Assignment;

public class SubmitAssignmentDto
{
    public string? Note { get; set; }
    public List<SubmissionFileDto> Files { get; set; } = [];
}
