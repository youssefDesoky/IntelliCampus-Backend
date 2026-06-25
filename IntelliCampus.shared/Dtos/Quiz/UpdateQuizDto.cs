namespace IntelliCampus.shared.Dtos.Quiz;

public class UpdateQuizDto
{
    public string? Title { get; set; }
    public string? Description { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? DueDate { get; set; }
    public int? DurationMinutes { get; set; }
    public decimal? MaxGrade { get; set; }
}
