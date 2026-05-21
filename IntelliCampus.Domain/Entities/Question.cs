namespace IntelliCampus.Domain.Entities;

public class Question
{
    public int Id { get; set; }
    public int QuizId { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Prompt { get; set; } = string.Empty;
    public string? Options { get; set; }
    public decimal Points { get; set; }
    public string? CorrectAnswer { get; set; }

    public Quiz Quiz { get; set; } = null!;
}
