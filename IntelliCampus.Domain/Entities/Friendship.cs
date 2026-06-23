namespace IntelliCampus.Domain.Entities;

public class Friendship
{
    public int FriendshipId { get; set; }
    public int UserId1 { get; set; }
    public int UserId2 { get; set; }
    public DateTime CreatedAt { get; set; } = EgyptTime.Now;

    public User User1 { get; set; } = null!;
    public User User2 { get; set; } = null!;
}
