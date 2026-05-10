namespace IntelliCampus.Domain.Entities;

public class Community
{
    public int CommunityId { get; set; }

    // Navigation properties
    public ICollection<Post> Posts { get; set; } = [];
}
