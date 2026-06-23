namespace IntelliCampus.Domain.Entities;

public class Group
{
    public int GroupId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ProfileImage { get; set; }
    public int CreatedById { get; set; }
    public DateTime CreatedAt { get; set; } = EgyptTime.Now;

    public User CreatedBy { get; set; } = null!;
    public ICollection<GroupMember> Members { get; set; } = [];
}
