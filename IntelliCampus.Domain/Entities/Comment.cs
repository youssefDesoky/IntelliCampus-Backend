namespace IntelliCampus.Domain.Entities;

public class Comment
{
    public int CommentId { get; set; }
    public string Content { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public int UserId { get; set; }
    public int PostId { get; set; }

    // Navigation properties
    public User User { get; set; } = null!;
    public Post Post { get; set; } = null!;
}
