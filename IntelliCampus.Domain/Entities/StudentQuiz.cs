namespace IntelliCampus.Domain.Entities;

public class StudentQuiz
{
    public int StudentId { get; set; }
    public int QuizId { get; set; }
    public decimal? Score { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime SubmittedAt { get; set; }
    public bool IsLate { get; set; }
    public string? AnswersJson { get; set; }
    public string? QuestionResultsJson { get; set; }

    // Navigation properties
    public Student Student { get; set; } = null!;
    public Quiz Quiz { get; set; } = null!;
}
