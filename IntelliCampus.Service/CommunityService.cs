using IntelliCampus.Domain.Entities;
using IntelliCampus.Domain.Entities.Enums;
using IntelliCampus.Domain.Interfaces;
using IntelliCampus.Service.Specifications;
using IntelliCampus.Service_Abstraction;
using IntelliCampus.Service.Exceptions;
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

    private IGenericRepository<User, int> Users
        => _unitOfWork.GetRepository<User, int>();

    public async Task<Post> CreateQuestionPostAsync(int courseId, int userId, string content)
    {
        var community = await GetOrCreateCommunityAsync(courseId);

        var post = new Post
        {
            Content = content,
            CreatedAt = EgyptTime.Now,
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
        if (post is null) throw new PostNotFoundException($"Post {postId} not found.");

        var course = await Courses.GetByIdAsync(courseId);
        if (course is null)
            throw new CourseNotFoundException(courseId);

        var courseCode = course.CourseCode;

        var question = new QuestionRequest(
            QuestionId: post.PostId.ToString(),
            Text: post.Content,
            CourseId: courseCode
        );

        try
        {
            return await _routingClient.RouteAsync(question);
        }
        catch (RouterNotInitializedException)
        {
            _logger.LogInformation("Router not initialized for course {CourseId}, initializing on demand...", courseId);
            await EnsureRouterInitializedAsync(courseId);
            return await _routingClient.RouteAsync(question);
        }
    }

    private async Task<Community> GetOrCreateCommunityAsync(int courseId)
    {
        var spec = new CommunityByCourseSpec(courseId);
        var community = await Communities.GetByIdAsync(spec);

        if (community is not null)
            return community;

        var course = await Courses.GetByIdAsync(courseId);
        if (course is null)
            throw new CourseNotFoundException($"Course {courseId} not found.");

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

            var postCandidatesRepo = _unitOfWork.GetRepository<PostCandidate, int>();
            for (int i = 0; i < topCandidates.Count; i++)
            {
                if (!int.TryParse(topCandidates[i].StudentId, out var uid)) continue;
                postCandidatesRepo.Add(new PostCandidate
                {
                    PostId = post.PostId,
                    UserId = uid,
                    Score = topCandidates[i].Score,
                    Rank = i + 1,
                    CreatedAt = EgyptTime.Now,
                });
            }
            await _unitOfWork.SaveChangesAsync();

            var message = "You are qualified to answer this question";
            await _notificationService.SendToManyAsync(userIds, NotificationType.QuestionRouting, message, clickUrl: $"/courses/{courseId}/community/questions/{post.PostId}");

            _logger.LogInformation(
                "Notified {Count} candidates for post {PostId} (branch={Branch})",
                userIds.Count, post.PostId, routingResponse.Branch);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to notify candidates for post {PostId}", post.PostId);
        }
    }

    private async Task EnsureRouterInitializedAsync(int courseId)
    {
        var course = await Courses.GetByIdAsync(new CourseSpec(courseId));
        if (course is null || course.StudentCourses.Count == 0) return;

        var courseCode = course.CourseCode ?? course.CourseId.ToString();

        var prereqEdges = course.Prerequisites
            .Select(p => new List<string>
            {
                p.PrerequisiteCourse.CourseCode ?? p.PrerequisiteCourseId.ToString(),
                courseCode,
            })
            .ToList();

        var students = course.StudentCourses
            .Select(sc => new StudentData(
                StudentId: sc.Student.UserId.ToString(),
                Name: sc.Student.FullName,
                Performance: CalculatePerformance(sc.Student, course, course.Grades),
                CompletedTopics: new List<string> { courseCode }
            ))
            .DistinctBy(s => s.StudentId)
            .ToList();

        var community = await Communities.GetByIdAsync(new CommunityByCourseSpec(courseId));

        var archivedQuestions = new List<QuestionRequest>();
        var interactions = new List<InteractionData>();
        var answers = new List<AnswerData>();

        if (community is not null)
        {
            var posts = await Posts.GetAllAsync(new CommunityPostSpec(community.CommunityId));

            foreach (var post in posts)
            {
                archivedQuestions.Add(new QuestionRequest(post.PostId.ToString(), post.Content, courseCode));

                interactions.Add(new InteractionData(post.UserId.ToString(), courseCode, "comment"));

                foreach (var comment in post.Comments)
                {
                    answers.Add(new AnswerData(
                        comment.CommentId.ToString(), post.PostId.ToString(),
                        comment.UserId.ToString()));

                    interactions.Add(new InteractionData(comment.UserId.ToString(), courseCode, "comment"));
                }
            }
        }

        var request = new InitializeRequest(
            CourseId: courseCode,
            PrereqEdges: prereqEdges.Count > 0 ? prereqEdges : null,
            ArchivedQuestions: archivedQuestions,
            Interactions: interactions,
            Answers: answers,
            Students: students
        );

        await _routingClient.InitializeAsync(request);
    }

    private static double CalculatePerformance(Student student, Course course,
        ICollection<Grade> allGrades)
    {
        var studentGrades = allGrades
            .Where(g => g.StudentId == student.UserId && g.CourseId == course.CourseId
                        && g.Status == "Graded")
            .ToList();

        if (studentGrades.Count == 0)
            return 0.5;

        var weightedSum = studentGrades.Sum(g => (double)(g.Score * g.Weight / 100));
        return Math.Clamp(weightedSum / 100.0, 0.0, 1.0);
    }

    public async Task<string> ExportCourseGraphAsync(int courseId, string graphType = "interaction")
    {
        var course = await Courses.GetByIdAsync(courseId);
        if (course is null)
            throw new CourseNotFoundException(courseId);

        var courseCode = course.CourseCode;
        return await _routingClient.ExportGraphAsync(courseCode, graphType);
    }

    public async Task<Post> GetQuestionPostAsync(int courseId, int postId)
    {
        var spec = new PostWithDetailsSpec(postId);
        var post = await Posts.GetByIdAsync(spec);
        if (post is null) throw new PostNotFoundException($"Post {postId} not found.");
        return post;
    }

    public async Task<Post> UpdatePostAsync(int postId, int userId, string newContent)
    {
        var spec = new PostWithDetailsSpec(postId);
        var post = await Posts.GetByIdAsync(spec);
        if (post is null) throw new PostNotFoundException($"Post {postId} not found.");
        if (post.UserId != userId)
            throw new UnauthorizedAccessException("You can only update your own posts.");

        post.Content = newContent;
        Posts.Update(post);
        await _unitOfWork.SaveChangesAsync();
        return post;
    }

    public async Task DeletePostAsync(int postId, int userId)
    {
        var spec = new PostWithDetailsSpec(postId);
        var post = await Posts.GetByIdAsync(spec);
        if (post is null) throw new PostNotFoundException($"Post {postId} not found.");

        if (post.UserId != userId && !await IsUserCourseInstructor(userId, post.Community.CourseId))
            throw new UnauthorizedAccessException("You can only delete your own posts.");

        Posts.Delete(post);
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task<Comment> AddCommentAsync(int postId, int userId, string content)
    {
        var post = await Posts.GetByIdAsync(postId);
        if (post is null)
            throw new PostNotFoundException($"Post {postId} not found.");

        var comment = new Comment
        {
            PostId = postId,
            UserId = userId,
            Content = content,
            CreatedAt = EgyptTime.Now,
        };

        var comments = _unitOfWork.GetRepository<Comment, int>();
        comments.Add(comment);
        await _unitOfWork.SaveChangesAsync();
        return comment;
    }

    public async Task<bool> ToggleUpvoteAsync(int postId, int userId)
    {
        var post = await Posts.GetByIdAsync(postId);
        if (post is null)
            throw new PostNotFoundException($"Post {postId} not found.");

        var user = await Users.GetByIdAsync(userId);
        if (user is null)
            throw new UserNotFoundException(userId);

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
            CreatedAt = EgyptTime.Now,
        });
        await _unitOfWork.SaveChangesAsync();
        return true;
    }

    public async Task DeleteCommentAsync(int commentId, int userId)
    {
        var comments = _unitOfWork.GetRepository<Comment, int>();
        var comment = await comments.GetByIdAsync(commentId);
        if (comment is null) throw new CommentNotFoundException($"Comment {commentId} not found.");
        if (comment.UserId != userId)
            throw new UnauthorizedAccessException("You can only delete your own comments.");

        comments.Delete(comment);
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task<Dictionary<int, string>> GetCourseInstructorRolesAsync(int courseId, IEnumerable<int> userIds)
    {
        var userIdsList = userIds.ToList();
        if (userIdsList.Count == 0) return new Dictionary<int, string>();

        var classes = _unitOfWork.GetRepository<Class, int>();
        var spec = new CourseInstructorsSpec(courseId, userIdsList);
        var courseClasses = await classes.GetAllAsync(spec);

        var result = new Dictionary<int, string>();
        foreach (var c in courseClasses)
        {
            if (c.InstructorId is null || c.Instructor is null) continue;
            if (result.ContainsKey(c.InstructorId.Value)) continue;

            var label = c.Instructor.InstructorRole switch
            {
                InstructorRole.TeachingAssistant => "TA",
                InstructorRole.Lecturer => "Lecturer",
                InstructorRole.AssistantLecturer => "Asst. Lecturer",
                InstructorRole.AssociateProfessor => "Assoc. Professor",
                InstructorRole.Professor => "Professor",
                _ => null
            };
            if (label is not null)
                result[c.InstructorId.Value] = label;
        }
        return result;
    }

    private async Task<bool> IsUserCourseInstructor(int userId, int courseId)
    {
        var classes = _unitOfWork.GetRepository<Class, int>();
        return await classes.AnyAsync(c => c.CourseId == courseId && c.InstructorId == userId);
    }
}
