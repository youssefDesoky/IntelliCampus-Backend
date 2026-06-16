using IntelliCampus.Domain.Entities;
using IntelliCampus.Domain.Entities.Enums;
using IntelliCampus.Domain.Interfaces;
using IntelliCampus.Service.Specifications;
using IntelliCampus.Service_Abstraction;
using IntelliCampus.Shared.Dtos.Routing;
using Microsoft.Extensions.Logging;

namespace IntelliCampus.Service;

public class CommunityService : ICommunityService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IRoutingClientService _routingClient;
    private readonly INotificationService _notificationService;
    private readonly ILogger<CommunityService> _logger;

    private static readonly int NotificationTopN = 3;

    public CommunityService(
        IUnitOfWork unitOfWork,
        IRoutingClientService routingClient,
        INotificationService notificationService,
        ILogger<CommunityService> logger)
    {
        _unitOfWork = unitOfWork;
        _routingClient = routingClient;
        _notificationService = notificationService;
        _logger = logger;
    }

    private IGenericRepository<Community, int> Communities
        => _unitOfWork.GetRepository<Community, int>();

    private IGenericRepository<Post, int> Posts
        => _unitOfWork.GetRepository<Post, int>();

    private IGenericRepository<Course, int> Courses
        => _unitOfWork.GetRepository<Course, int>();

    public async Task<Post> CreateQuestionPostAsync(int courseId, int userId, string content)
    {
        var community = await GetOrCreateCommunityAsync(courseId);

        var post = new Post
        {
            Content = content,
            CreatedAt = DateTime.UtcNow,
            IsPinned = false,
            CommunityId = community.CommunityId,
            UserId = userId,
        };

        Posts.Add(post);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation(
            "Question post {PostId} created in community for course {CourseId}",
            post.PostId, courseId);

        // Route the question and notify top candidates
        await NotifyTopCandidatesAsync(courseId, post);

        return post;
    }

    public async Task<IEnumerable<Post>> GetCoursePostsAsync(int courseId)
    {
        var community = await GetOrCreateCommunityAsync(courseId);
        var spec = new CommunityPostSpec(community.CommunityId);
        return await Posts.GetAllAsync(spec);
    }

    public async Task<RoutingResponse?> RouteQuestionAsync(int courseId, int postId, int topN = 3)
    {
        var post = await Posts.GetByIdAsync(postId);
        if (post is null) return null;

        var course = await Courses.GetByIdAsync(courseId);
        var courseCode = course?.CourseCode ?? courseId.ToString();

        var question = new QuestionRequest(
            QuestionId: post.PostId.ToString(),
            Text: post.Content,
            CourseId: courseCode
        );

        return await _routingClient.RouteAsync(question);
    }

    private async Task<Community> GetOrCreateCommunityAsync(int courseId)
    {
        var spec = new CommunityByCourseSpec(courseId);
        var community = await Communities.GetByIdAsync(spec);

        if (community is not null)
            return community;

        var course = await Courses.GetByIdAsync(courseId);
        if (course is null)
            throw new InvalidOperationException($"Course {courseId} not found.");

        community = new Community { CourseId = courseId };
        Communities.Add(community);
        await _unitOfWork.SaveChangesAsync();
        return community;
    }

    private async Task NotifyTopCandidatesAsync(int courseId, Post post)
    {
        try
        {
            var routingResponse = await RouteQuestionAsync(courseId, post.PostId);

            if (routingResponse is null)
            {
                _logger.LogWarning("Routing returned no response for post {PostId}", post.PostId);
                return;
            }

            var topCandidates = routingResponse.Ranked
                .Where(c => int.TryParse(c.StudentId, out var uid) && uid != post.UserId)
                .Take(NotificationTopN).ToList();
            if (topCandidates.Count == 0) return;

            var userIds = new List<int>();
            var candidateDetails = new List<string>();

            foreach (var c in topCandidates)
            {
                if (!int.TryParse(c.StudentId, out var uid)) continue;
                userIds.Add(uid);
                candidateDetails.Add($"ID={c.StudentId}, Score={c.Score:F4}");
            }

            if (userIds.Count == 0) return;

            _logger.LogInformation(
                "Top candidates for post {PostId} (branch={Branch}): {Candidates}",
                post.PostId, routingResponse.Branch, string.Join("; ", candidateDetails));

            var message = $"You are a top candidate to answer: \"{post.Content}\"";
            await _notificationService.SendToManyAsync(userIds, NotificationType.QuestionRouting, message);

            _logger.LogInformation(
                "Notified {Count} candidates for post {PostId} (branch={Branch})",
                userIds.Count, post.PostId, routingResponse.Branch);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to notify candidates for post {PostId}", post.PostId);
        }
    }

    public async Task<string> ExportCourseGraphAsync(int courseId, string graphType = "interaction")
    {
        var course = await Courses.GetByIdAsync(courseId);
        var courseCode = course?.CourseCode ?? courseId.ToString();
        return await _routingClient.ExportGraphAsync(courseCode, graphType);
    }

    public async Task<Post?> UpdatePostAsync(int postId, int userId, string newContent)
    {
        var spec = new PostWithDetailsSpec(postId);
        var post = await Posts.GetByIdAsync(spec);
        if (post is null || post.UserId != userId) return null;

        post.Content = newContent;
        Posts.Update(post);
        await _unitOfWork.SaveChangesAsync();
        return post;
    }

    public async Task<bool> DeletePostAsync(int postId, int userId)
    {
        var spec = new PostWithDetailsSpec(postId);
        var post = await Posts.GetByIdAsync(spec);
        if (post is null) return false;

        if (post.UserId != userId && !await IsUserCourseInstructor(userId, post.Community.CourseId))
            return false;

        Posts.Delete(post);
        await _unitOfWork.SaveChangesAsync();
        return true;
    }

    public async Task<Comment> AddCommentAsync(int postId, int userId, string content)
    {
        var comment = new Comment
        {
            PostId = postId,
            UserId = userId,
            Content = content,
            CreatedAt = DateTime.UtcNow,
        };

        var comments = _unitOfWork.GetRepository<Comment, int>();
        comments.Add(comment);
        await _unitOfWork.SaveChangesAsync();
        return comment;
    }

    public async Task<bool> ToggleUpvoteAsync(int postId, int userId)
    {
        var votes = _unitOfWork.GetRepository<PostVote, int>();
        var existing = await votes.GetByIdAsync(
            new PostVoteByUserSpec(postId, userId));

        if (existing is not null)
        {
            votes.Delete(existing);
            await _unitOfWork.SaveChangesAsync();
            return false;
        }

        votes.Add(new PostVote
        {
            PostId = postId,
            UserId = userId,
            CreatedAt = DateTime.UtcNow,
        });
        await _unitOfWork.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteCommentAsync(int commentId, int userId)
    {
        var comments = _unitOfWork.GetRepository<Comment, int>();
        var comment = await comments.GetByIdAsync(commentId);
        if (comment is null) return false;
        if (comment.UserId != userId) return false;

        comments.Delete(comment);
        await _unitOfWork.SaveChangesAsync();
        return true;
    }

    private async Task<bool> IsUserCourseInstructor(int userId, int courseId)
    {
        var classes = _unitOfWork.GetRepository<Class, int>();
        return await classes.AnyAsync(c => c.CourseId == courseId && c.InstructorId == userId);
    }
}
