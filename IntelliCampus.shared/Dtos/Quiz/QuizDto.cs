using System.Text.Json.Serialization;

namespace IntelliCampus.shared.Dtos.Quiz;

public class QuizDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime DueDate { get; set; }
    public int DurationMinutes { get; set; }
    public decimal MaxScore { get; set; }
    public int CourseId { get; set; }
    public string? CourseName { get; set; }
    public string Status { get; set; } = "Upcoming";
}
