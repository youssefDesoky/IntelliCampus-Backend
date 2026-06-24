using System.Security.Claims;
using IntelliCampus.Shared.Dtos.Routing;
using IntelliCampus.Service_Abstraction;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IntelliCampus.Web.Controllers;

[ApiController]
[Route("api/courses/{courseId}/community")]
[Authorize]
public class CommunityController(
    ICommunityService communityService)
    : ControllerBase
{
    private int UserId
        => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpPost("questions")]
    public async Task<IActionResult> CreateQuestion(int courseId, [FromBody] CreateQuestionRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Content))
            return BadRequest("Question content is required.");

        var post = await communityService.CreateQuestionPostAsync(courseId, UserId, request.Content);

        return Ok(new
        {
            postId = post.PostId,
            content = post.Content,
            createdAt = post.CreatedAt,
        });
    }

    [HttpGet("questions")]
    public async Task<IActionResult> GetQuestions(int courseId)
    {
        var posts = (await communityService.GetCoursePostsAsync(courseId)).ToList();

        var allCommenterIds = posts.SelectMany(p => p.Comments).Select(c => c.UserId).Distinct().ToList();
        var instructorRoles = await communityService.GetCourseInstructorRolesAsync(courseId, allCommenterIds);

        return Ok(posts.Select(p =>
        {
            var candidateMap = p.Candidates.ToDictionary(c => c.UserId, c => c.Rank);
            return new
            {
                p.PostId,
                p.Content,
                p.CreatedAt,
                p.IsPinned,
                userId = p.UserId,
                authorName = p.User.FullName,
                commentCount = p.Comments.Count,
                upvoteCount = p.Votes.Count,
                comments = p.Comments.Select(c => new
                {
                    c.CommentId,
                    c.Content,
                    c.CreatedAt,
                    userId = c.UserId,
                    authorName = c.User.FullName,
                    isRecommended = candidateMap.ContainsKey(c.UserId),
                    recommendationRank = candidateMap.TryGetValue(c.UserId, out var rank) ? rank : (int?)null,
                    instructorRole = instructorRoles.TryGetValue(c.UserId, out var role) ? role : null,
                }),
            };
        }));
    }

    [HttpGet("questions/{postId}")]
    public async Task<IActionResult> GetQuestion(int courseId, int postId)
    {
        var post = await communityService.GetQuestionPostAsync(courseId, postId);

        var commenterIds = post.Comments.Select(c => c.UserId).Distinct().ToList();
        var instructorRoles = await communityService.GetCourseInstructorRolesAsync(courseId, commenterIds);
        var candidateMap = post.Candidates.ToDictionary(c => c.UserId, c => c.Rank);

        return Ok(new
        {
            post.PostId,
            post.Content,
            post.CreatedAt,
            post.IsPinned,
            userId = post.UserId,
            authorName = post.User.FullName,
            commentCount = post.Comments.Count,
            upvoteCount = post.Votes.Count,
            comments = post.Comments.Select(c => new
            {
                c.CommentId,
                c.Content,
                c.CreatedAt,
                userId = c.UserId,
                authorName = c.User.FullName,
                isRecommended = candidateMap.ContainsKey(c.UserId),
                recommendationRank = candidateMap.TryGetValue(c.UserId, out var rank) ? rank : (int?)null,
                instructorRole = instructorRoles.TryGetValue(c.UserId, out var role) ? role : null,
            }),
        });
    }

    [HttpGet("graph")]
    public async Task<IActionResult> ExportGraph(int courseId, [FromQuery] string graphType = "interaction")
    {
        var gexf = await communityService.ExportCourseGraphAsync(courseId, graphType);
        return File(
            System.Text.Encoding.UTF8.GetBytes(gexf),
            "application/xml",
            fileDownloadName: $"{courseId}_{graphType}.gexf");
    }

    [HttpPost("questions/{postId}")]
    public async Task<IActionResult> UpdateQuestion(int postId, [FromBody] CreateQuestionRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Content))
            return BadRequest("Content is required.");

        var post = await communityService.UpdatePostAsync(postId, UserId, request.Content);
        return Ok(new { post.PostId, post.Content });
    }

    [HttpDelete("questions/{postId}")]
    public async Task<IActionResult> DeleteQuestion(int postId)
    {
        await communityService.DeletePostAsync(postId, UserId);
        return NoContent();
    }

    [HttpPost("questions/{postId}/comments")]
    public async Task<IActionResult> AddComment(int postId, [FromBody] CreateCommentRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Content))
            return BadRequest("Comment content is required.");

        var comment = await communityService.AddCommentAsync(postId, UserId, request.Content);
        return Ok(new
        {
            comment.CommentId,
            comment.Content,
            comment.CreatedAt,
            userId = comment.UserId,
        });
    }

    [HttpDelete("comments/{commentId}")]
    public async Task<IActionResult> DeleteComment(int commentId)
    {
        await communityService.DeleteCommentAsync(commentId, UserId);
        return NoContent();
    }

    [HttpPost("questions/{postId}/upvote")]
    public async Task<IActionResult> ToggleUpvote(int postId)
    {
        var voted = await communityService.ToggleUpvoteAsync(postId, UserId);
        return Ok(new { upvoted = voted });
    }

    [HttpPost("route")]
    public async Task<IActionResult> RouteQuestion(int courseId, [FromBody] RouteQuestionRequest request)
    {
        var result = await communityService.RouteQuestionAsync(courseId, request.PostId, request.TopN ?? 3);
        return Ok(result);
    }
}

public record CreateQuestionRequest(string Content);

public record RouteQuestionRequest(int PostId, int? TopN = 3);

public record CreateCommentRequest(string Content);
