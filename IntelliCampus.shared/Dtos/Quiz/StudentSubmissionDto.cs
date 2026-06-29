using System.Text.Json.Serialization;

namespace IntelliCampus.shared.Dtos.Quiz;

public class StudentSubmissionDto
{
    public int StudentId { get; set; }

    public string? StudentName { get; set; }

    public decimal? Score { get; set; }

    public decimal MaxScore { get; set; }

    public string SubmittedAt { get; set; } = string.Empty;

    public Dictionary<string, object>? Answers { get; set; }

    public List<QuestionResultDto>? QuestionResults { get; set; }

    public List<SubmissionAnswerDetailDto> AnswerDetails { get; set; } = new();
}
