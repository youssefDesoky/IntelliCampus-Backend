namespace IntelliCampus.Domain.Entities;

public class GroupMember
{
    public int GroupMemberId { get; set; }
    public int GroupId { get; set; }
    public int UserId { get; set; }
    public DateTime JoinedAt { get; set; } = EgyptTime.Now;

    public Group Group { get; set; } = null!;
    public User User { get; set; } = null!;
}
