using IntelliCampus.DAL.Entities.Enums;

namespace IntelliCampus.DAL.Entities;

public class Class
{
    public int ClassId { get; set; }
    public ClassType ClassType { get; set; }
    public int CourseId { get; set; }

    // Navigation properties
    public Course Course { get; set; } = null!;
    public ICollection<Session> Sessions { get; set; } = [];
}
