namespace IntelliCampus.Shared.Dtos.Grade;

public class CourseWorkWeightDto
{
    public int CourseId { get; set; }
    public decimal QuizWeight { get; set; }
    public decimal AssignmentWeight { get; set; }
    public decimal MidtermWeight { get; set; }
}
