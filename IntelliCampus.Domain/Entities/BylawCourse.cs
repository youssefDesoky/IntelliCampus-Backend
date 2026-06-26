using IntelliCampus.Domain.Entities.Enums;

namespace IntelliCampus.Domain.Entities;

public class BylawCourse
{
    public int BylawCourseId { get; set; }
    public int BylawId { get; set; }
    public int CourseId { get; set; }
    public CourseType CourseType { get; set; }
    public int? CreditHours { get; set; }
    public string? AllowedDepartmentIds { get; set; }
    public Bylaw Bylaw { get; set; } = null!;
    public Course Course { get; set; } = null!;
    public ICollection<BylawCoursePrerequisite> Prerequisites { get; set; } = new List<BylawCoursePrerequisite>();
    public ICollection<BylawCoursePrerequisite> PrerequisiteFor { get; set; } = new List<BylawCoursePrerequisite>();
}
