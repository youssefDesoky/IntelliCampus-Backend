namespace IntelliCampus.Domain.Entities;

public class PostVote
{
    public int PostVoteId { get; set; }
    public int PostId { get; set; }
    public int UserId { get; set; }
    public DateTime CreatedAt { get; set; }

    public Post Post { get; set; } = null!;
    public User User { get; set; } = null!;
}
