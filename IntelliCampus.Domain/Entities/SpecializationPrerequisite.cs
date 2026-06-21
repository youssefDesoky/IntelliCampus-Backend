namespace IntelliCampus.Domain.Entities;

public class SpecializationPrerequisite
{
    public int SpecializationId { get; set; }
    public int CourseId { get; set; }
    public decimal MinGrade { get; set; }

    public Specialization Specialization { get; set; } = null!;
    public Course Course { get; set; } = null!;
}
