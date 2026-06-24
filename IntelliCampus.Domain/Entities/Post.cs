namespace IntelliCampus.Domain.Entities;

public class Post
{
    public int PostId { get; set; }
    public string Content { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public bool IsPinned { get; set; }
    public int CommunityId { get; set; }
    public int UserId { get; set; }

    // Navigation properties
    public Community Community { get; set; } = null!;
    public User User { get; set; } = null!;
    public ICollection<Comment> Comments { get; set; } = [];
    public ICollection<PostVote> Votes { get; set; } = [];
    public ICollection<PostCandidate> Candidates { get; set; } = [];
}
