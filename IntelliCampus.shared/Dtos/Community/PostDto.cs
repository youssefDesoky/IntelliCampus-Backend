namespace IntelliCampus.Shared.Dtos.Community;

public class PostDto
{
    public int PostId { get; set; }
    public string Content { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public bool IsPinned { get; set; }
    public int UserId { get; set; }
    public string AuthorName { get; set; } = null!;
    public string? AuthorNameAr { get; set; }
    public string? AuthorProfileImage { get; set; }
    public int CommentCount { get; set; }
    public int UpvoteCount { get; set; }
    public bool IsUpvoted { get; set; }
    public bool CanEdit { get; set; }
    public bool CanDelete { get; set; }
    public List<CommunityCommentDto> Comments { get; set; } = [];
}

public class CommunityCommentDto
{
    public int CommentId { get; set; }
    public string Content { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public int UserId { get; set; }
    public string AuthorName { get; set; } = null!;
    public string? AuthorNameAr { get; set; }
    public string? AuthorProfileImage { get; set; }
    public bool IsRecommended { get; set; }
    public int? RecommendationRank { get; set; }
    public string? InstructorRole { get; set; }
}
