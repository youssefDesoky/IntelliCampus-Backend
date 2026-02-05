namespace IntelliCampus.DAL.Entities;

public class Quiz
{
    public int QuizId { get; set; }
    public int TotalMarks { get; set; }

    // Navigation properties
    public ICollection<StudentQuiz> StudentQuizzes { get; set; } = [];
}
