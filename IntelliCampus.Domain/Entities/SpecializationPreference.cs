namespace IntelliCampus.Domain.Entities;

public class SpecializationPreference
{
    public int Id { get; set; }
    public int StudentId { get; set; }
    public string TargetType { get; set; } = null!;
    public int TargetId { get; set; }
    public int Rank { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public Student Student { get; set; } = null!;
}
