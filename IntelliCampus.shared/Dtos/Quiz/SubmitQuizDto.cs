namespace IntelliCampus.shared.Dtos.Quiz;

public class SubmitQuizDto
{
    public int QuizId { get; set; }
    public Dictionary<string, object> Answers { get; set; } = new();
}
