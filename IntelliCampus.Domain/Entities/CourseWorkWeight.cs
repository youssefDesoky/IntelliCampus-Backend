namespace IntelliCampus.Domain.Entities;

public class CourseWorkWeight
{
    public int CourseId { get; set; }
    public decimal QuizWeight { get; set; }
    public decimal AssignmentWeight { get; set; }
    public decimal MidtermWeight { get; set; }

    public Course Course { get; set; } = null!;
}
