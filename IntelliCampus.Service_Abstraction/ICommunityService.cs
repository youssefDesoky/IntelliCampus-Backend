using IntelliCampus.Domain.Entities;
using IntelliCampus.Shared.Dtos.Routing;

namespace IntelliCampus.Service_Abstraction;

public interface ICommunityService
{
    Task<Post> CreateQuestionPostAsync(int courseId, int userId, string content);
    Task<IEnumerable<Post>> GetCoursePostsAsync(int courseId);
    Task<RoutingResponse?> RouteQuestionAsync(int courseId, int postId, int topN = 3);
    Task<string> ExportCourseGraphAsync(int courseId, string graphType = "interaction");
    Task<Post?> UpdatePostAsync(int postId, int userId, string newContent);
    Task<bool> DeletePostAsync(int postId, int userId);
    Task<Comment> AddCommentAsync(int postId, int userId, string content);
    Task<bool> ToggleUpvoteAsync(int postId, int userId);
    Task<bool> DeleteCommentAsync(int commentId, int userId);
}
