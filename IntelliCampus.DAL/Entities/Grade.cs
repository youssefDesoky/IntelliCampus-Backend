using IntelliCampus.DAL.Entities.Enums;

namespace IntelliCampus.DAL.Entities;

public class Grade
{
    public int GradeId { get; set; }
    public GradeType Type { get; set; }
    public decimal Score { get; set; }
    public int CourseId { get; set; }
    public int StudentId { get; set; }

    // Navigation properties
    public Course Course { get; set; } = null!;
    public Student Student { get; set; } = null!;
}
