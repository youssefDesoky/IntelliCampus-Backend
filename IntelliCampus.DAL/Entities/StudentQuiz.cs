namespace IntelliCampus.DAL.Entities;

public class StudentQuiz
{
    public int StudentId { get; set; }
    public int QuizId { get; set; }
    public decimal? Score { get; set; }

    // Navigation properties
    public Student Student { get; set; } = null!;
    public Quiz Quiz { get; set; } = null!;
}
