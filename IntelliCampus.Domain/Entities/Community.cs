namespace IntelliCampus.Domain.Entities;

public class Community
{
    public int CommunityId { get; set; }
    public int CourseId { get; set; }

    // Navigation properties
    public Course Course { get; set; } = null!;
    public ICollection<Post> Posts { get; set; } = [];
}
