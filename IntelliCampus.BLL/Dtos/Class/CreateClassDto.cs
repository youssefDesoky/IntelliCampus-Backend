using IntelliCampus.DAL.Entities.Enums;

namespace IntelliCampus.BLL.Dtos.Class;

public class CreateClassDto
{
    public ClassType ClassType { get; set; }
    public int CourseId { get; set; }
    public int? InstructorId { get; set; }
}
