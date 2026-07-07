namespace IntelliCampus.Domain.Entities;

public class DepartmentPreference
{
    public int Id { get; set; }
    public int StudentId { get; set; }
    public int DepartmentId { get; set; }
    public int Rank { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public Student Student { get; set; } = null!;
}
