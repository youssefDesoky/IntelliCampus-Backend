namespace IntelliCampus.shared.Dtos.Quiz;

public class StudentQuizDto
{
    public int StudentQuizId { get; set; }
    public int StudentId { get; set; }
    public string? StudentName { get; set; }
    public int QuizId { get; set; }
    public string? QuizTitle { get; set; }
    public decimal? Score { get; set; }
    public decimal MaxGrade { get; set; }
    public string SubmittedAt { get; set; } = string.Empty;
    public bool IsLate { get; set; }
}
