using IntelliCampus.DAL.Entities.Enums;

namespace IntelliCampus.BLL.Dtos.Class;

public class ClassDto
{
    public int ClassId { get; set; }
    public ClassType ClassType { get; set; }
    public string ClassTypeName => ClassType.ToString();
    public int CourseId { get; set; }
    public string CourseName { get; set; } = null!;
    public int? InstructorId { get; set; }
    public string? InstructorName { get; set; }
}
