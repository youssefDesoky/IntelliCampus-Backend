namespace IntelliCampus.Domain.Entities;

public class ChatbotQuery
{
    public int QueryId { get; set; }
    public DateTime Timestamp { get; set; }
    public string Question { get; set; } = null!;
    public string? Response { get; set; }
    public int StudentId { get; set; }

    // Navigation properties
    public Student Student { get; set; } = null!;
}
