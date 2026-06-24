namespace IntelliCampus.Domain.Entities;

public class PostCandidate
{
    public int Id { get; set; }
    public int PostId { get; set; }
    public int UserId { get; set; }
    public double Score { get; set; }
    public int Rank { get; set; }
    public DateTime CreatedAt { get; set; }

    public Post Post { get; set; } = null!;
    public User User { get; set; } = null!;
}
