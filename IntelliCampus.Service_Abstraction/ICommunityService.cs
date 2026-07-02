using IntelliCampus.Domain.Entities;
using IntelliCampus.shared.Pagination;
using IntelliCampus.Shared.Dtos.Community;
using IntelliCampus.Shared.Dtos.Routing;
using IntelliCampus.Shared.Params;

namespace IntelliCampus.Service_Abstraction;

public interface ICommunityService
{
    Task<Post> CreateQuestionPostAsync(int courseId, int userId, string content);
    Task<IEnumerable<Post>> GetCoursePostsAsync(int courseId);
    Task<PaginatedResult<Post>> GetCoursePostsAsync(int courseId, CommunityQueryParams queryParams);
    Task<PaginatedResult<PostDto>> GetCoursePostDtosAsync(int courseId, CommunityQueryParams queryParams, int currentUserId);
    Task<PostDto> GetQuestionPostDtoAsync(int courseId, int postId, int currentUserId);
    Task<RoutingResponse?> RouteQuestionAsync(int courseId, int postId, int topN = 3);
    Task<string> ExportCourseGraphAsync(int courseId, string graphType = "interaction");
    Task<Post> UpdatePostAsync(int postId, int userId, string newContent);
    Task DeletePostAsync(int postId, int userId);
    Task<Comment> AddCommentAsync(int postId, int userId, string content);
    Task<bool> ToggleUpvoteAsync(int postId, int userId);
    Task DeleteCommentAsync(int commentId, int userId);
    Task<Post> GetQuestionPostAsync(int courseId, int postId);
    Task<Dictionary<int, string>> GetCourseInstructorRolesAsync(int courseId, IEnumerable<int> userIds);
    string ResolveProfileImage(string? profileImage);
    Task<bool> IsUserCourseInstructorAsync(int userId, int courseId);
}
