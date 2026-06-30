using FluentAssertions;
using IntelliCampus.Domain.Entities;
using IntelliCampus.Domain.Entities.Enums;
using IntelliCampus.Domain.Interfaces;
using IntelliCampus.Service;
using IntelliCampus.Service_Abstraction;
using IntelliCampus.Shared.Dtos.Routing;
using IntelliCampus.Shared.Params;
using IntelliCampus.UnitTests.TestHelpers;
using Microsoft.Extensions.Logging;
using Moq;

namespace IntelliCampus.UnitTests.Services;

public class CommunityServiceTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IRoutingClientService> _routingClientMock;
    private readonly Mock<INotificationService> _notificationServiceMock;
    private readonly Mock<ILogger<CommunityService>> _loggerMock;
    private readonly Mock<IGenericRepository<Community, int>> _communityRepoMock;
    private readonly Mock<IGenericRepository<Post, int>> _postRepoMock;
    private readonly Mock<IGenericRepository<Course, int>> _courseRepoMock;
    private readonly Mock<IGenericRepository<User, int>> _userRepoMock;
    private readonly Mock<IGenericRepository<PostCandidate, int>> _postCandidateRepoMock;
    private readonly Mock<IGenericRepository<Comment, int>> _commentRepoMock;
    private readonly Mock<IGenericRepository<PostVote, int>> _voteRepoMock;
    private readonly Mock<IGenericRepository<Class, int>> _classRepoMock;
    private readonly CommunityService _sut;

    public CommunityServiceTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _routingClientMock = new Mock<IRoutingClientService>();
        _notificationServiceMock = new Mock<INotificationService>();
        _loggerMock = new Mock<ILogger<CommunityService>>();

        _communityRepoMock = new Mock<IGenericRepository<Community, int>>();
        _postRepoMock = new Mock<IGenericRepository<Post, int>>();
        _courseRepoMock = new Mock<IGenericRepository<Course, int>>();
        _userRepoMock = new Mock<IGenericRepository<User, int>>();
        _postCandidateRepoMock = new Mock<IGenericRepository<PostCandidate, int>>();
        _commentRepoMock = new Mock<IGenericRepository<Comment, int>>();
        _voteRepoMock = new Mock<IGenericRepository<PostVote, int>>();
        _classRepoMock = new Mock<IGenericRepository<Class, int>>();

        _unitOfWorkMock.Setup(u => u.GetRepository<Community, int>()).Returns(_communityRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.GetRepository<Post, int>()).Returns(_postRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.GetRepository<Course, int>()).Returns(_courseRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.GetRepository<User, int>()).Returns(_userRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.GetRepository<PostCandidate, int>()).Returns(_postCandidateRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.GetRepository<Comment, int>()).Returns(_commentRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.GetRepository<PostVote, int>()).Returns(_voteRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.GetRepository<Class, int>()).Returns(_classRepoMock.Object);

        _sut = new CommunityService(_unitOfWorkMock.Object, _routingClientMock.Object, _notificationServiceMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task CreateQuestionPostAsync_ExistingCourse_ReturnsPost()
    {
        var course = TestDataFactory.CourseFaker.Generate();
        var student = TestDataFactory.StudentFaker.Generate();
        var community = new Community { CommunityId = 1, CourseId = course.CourseId };
        Post? capturedPost = null;

        _communityRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Community>>())).ReturnsAsync(community);
        _postRepoMock.Setup(r => r.Add(It.IsAny<Post>())).Callback<Post>(p => capturedPost = p);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        var result = await _sut.CreateQuestionPostAsync(course.CourseId, student.UserId, "Test question");

        result.Should().NotBeNull();
        result.Content.Should().Be("Test question");
        result.UserId.Should().Be(student.UserId);
        result.CommunityId.Should().Be(community.CommunityId);
        result.IsPinned.Should().BeFalse();

        capturedPost.Should().NotBeNull();
        capturedPost!.Content.Should().Be("Test question");
        capturedPost.UserId.Should().Be(student.UserId);
        capturedPost.CommunityId.Should().Be(community.CommunityId);
        capturedPost.IsPinned.Should().BeFalse();

        _postRepoMock.Verify(r => r.Add(It.IsAny<Post>()), Times.Once);
        _communityRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<Community>>()), Times.AtLeastOnce);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.AtLeastOnce);
        _courseRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task CreateQuestionPostAsync_NonExistingCourse_ThrowsCourseNotFoundException()
    {
        _communityRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Community>>())).ReturnsAsync((Community?)null);
        _courseRepoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Course?)null);

        await _sut.Invoking(s => s.CreateQuestionPostAsync(999, 1, "content"))
            .Should().ThrowAsync<CourseNotFoundException>();

        _postRepoMock.Verify(r => r.Add(It.IsAny<Post>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task CreateQuestionPostAsync_CreatesCommunityIfNotExists()
    {
        var course = TestDataFactory.CourseFaker.Generate();
        var student = TestDataFactory.StudentFaker.Generate();
        Community? capturedCommunity = null;
        Post? capturedPost = null;

        _communityRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Community>>())).ReturnsAsync((Community?)null);
        _courseRepoMock.Setup(r => r.GetByIdAsync(course.CourseId)).ReturnsAsync(course);
        _communityRepoMock.Setup(r => r.Add(It.IsAny<Community>())).Callback<Community>(c => capturedCommunity = c);
        _postRepoMock.Setup(r => r.Add(It.IsAny<Post>())).Callback<Post>(p => capturedPost = p);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        var result = await _sut.CreateQuestionPostAsync(course.CourseId, student.UserId, "New community post");

        result.Should().NotBeNull();
        result.Content.Should().Be("New community post");
        result.UserId.Should().Be(student.UserId);

        capturedCommunity.Should().NotBeNull();
        capturedCommunity!.CourseId.Should().Be(course.CourseId);

        capturedPost.Should().NotBeNull();
        capturedPost!.Content.Should().Be("New community post");

        _communityRepoMock.Verify(r => r.Add(It.IsAny<Community>()), Times.Once);
        _postRepoMock.Verify(r => r.Add(It.IsAny<Post>()), Times.Once);
        _courseRepoMock.Verify(r => r.GetByIdAsync(course.CourseId), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.AtLeastOnce);
    }

    [Fact]
    public async Task GetCoursePostsAsync_NoCommunity_ThrowsCourseNotFoundException()
    {
        _communityRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Community>>())).ReturnsAsync((Community?)null);
        _courseRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync((Course?)null);

        await _sut.Invoking(s => s.GetCoursePostsAsync(1))
            .Should().ThrowAsync<CourseNotFoundException>();
    }

    [Fact]
    public async Task GetQuestionPostAsync_NonExisting_ThrowsPostNotFoundException()
    {
        _postRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Post>>())).ReturnsAsync((Post?)null);

        await _sut.Invoking(s => s.GetQuestionPostAsync(1, 999))
            .Should().ThrowAsync<PostNotFoundException>();
    }

    [Fact]
    public async Task GetCoursePostsAsync_Paginated_ReturnsPaginatedResult()
    {
        var course = TestDataFactory.CourseFaker.Generate();
        var community = new Community { CommunityId = 1, CourseId = course.CourseId };
        var posts = Enumerable.Range(1, 5).Select(i => new Post { PostId = i, Content = $"Post {i}", CommunityId = community.CommunityId, UserId = i }).ToList();
        var queryParams = new CommunityQueryParams { PageIndex = 1, PageSize = 10 };

        _communityRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Community>>())).ReturnsAsync(community);
        _postRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<Post>>())).ReturnsAsync(posts);
        _postRepoMock.Setup(r => r.CountAsync(It.IsAny<ISpecifications<Post>>())).ReturnsAsync(20);

        var result = await _sut.GetCoursePostsAsync(course.CourseId, queryParams);

        result.Should().NotBeNull();
        result.PageIndex.Should().Be(1);
        result.PageSize.Should().Be(5);
        result.TotalCount.Should().Be(20);
        result.Data.Should().HaveCount(5);

        _communityRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<Community>>()), Times.Once);
        _postRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<Post>>()), Times.Once);
        _postRepoMock.Verify(r => r.CountAsync(It.IsAny<ISpecifications<Post>>()), Times.Once);
    }

    [Fact]
    public async Task RouteQuestionAsync_ExistingPostAndCourse_ReturnsRoutingResponse()
    {
        var course = TestDataFactory.CourseFaker.Generate();
        var post = TestDataFactory.PostFaker.Generate();
        post.PostId = 1;
        post.Content = "Test question?";
        var response = new RoutingResponse("branch", null, new List<RankedCandidate>());

        _postRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(post);
        _courseRepoMock.Setup(r => r.GetByIdAsync(course.CourseId)).ReturnsAsync(course);
        _routingClientMock.Setup(r => r.RouteAsync(It.IsAny<QuestionRequest>(), It.IsAny<CancellationToken>())).ReturnsAsync(response);

        var result = await _sut.RouteQuestionAsync(course.CourseId, 1);

        result.Should().NotBeNull();
        result!.Branch.Should().Be("branch");
        result.Ranked.Should().BeEmpty();

        _postRepoMock.Verify(r => r.GetByIdAsync(1), Times.Once);
        _courseRepoMock.Verify(r => r.GetByIdAsync(course.CourseId), Times.Once);
        _routingClientMock.Verify(r => r.RouteAsync(It.IsAny<QuestionRequest>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RouteQuestionAsync_PostNotFound_ThrowsPostNotFoundException()
    {
        _postRepoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Post?)null);

        await _sut.Invoking(s => s.RouteQuestionAsync(1, 999))
            .Should().ThrowAsync<PostNotFoundException>();

        _courseRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<int>()), Times.Never);
        _routingClientMock.Verify(r => r.RouteAsync(It.IsAny<QuestionRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RouteQuestionAsync_CourseNotFound_ThrowsCourseNotFoundException()
    {
        var post = TestDataFactory.PostFaker.Generate();
        post.PostId = 1;
        _postRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(post);
        _courseRepoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Course?)null);

        await _sut.Invoking(s => s.RouteQuestionAsync(999, 1))
            .Should().ThrowAsync<CourseNotFoundException>();

        _routingClientMock.Verify(r => r.RouteAsync(It.IsAny<QuestionRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RouteQuestionAsync_RetriesOnRouterNotInitializedException()
    {
        var course = TestDataFactory.CourseFaker.Generate();
        var post = TestDataFactory.PostFaker.Generate();
        post.PostId = 1;
        post.Content = "Test question?";
        var response = new RoutingResponse("retry-branch", null, new List<RankedCandidate>());

        _postRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(post);
        _courseRepoMock.Setup(r => r.GetByIdAsync(course.CourseId)).ReturnsAsync(course);
        _routingClientMock.SetupSequence(r => r.RouteAsync(It.IsAny<QuestionRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new RouterNotInitializedException("1"))
            .ReturnsAsync(response);

        var result = await _sut.RouteQuestionAsync(course.CourseId, 1);

        result.Should().NotBeNull();
        result!.Branch.Should().Be("retry-branch");

        _routingClientMock.Verify(r => r.RouteAsync(It.IsAny<QuestionRequest>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task ExportCourseGraphAsync_ExistingCourse_ReturnsGraphString()
    {
        var course = TestDataFactory.CourseFaker.Generate();
        _courseRepoMock.Setup(r => r.GetByIdAsync(course.CourseId)).ReturnsAsync(course);
        _routingClientMock.Setup(r => r.ExportGraphAsync(course.CourseCode, "interaction", It.IsAny<CancellationToken>())).ReturnsAsync("graph-data");

        var result = await _sut.ExportCourseGraphAsync(course.CourseId);

        result.Should().Be("graph-data");

        _courseRepoMock.Verify(r => r.GetByIdAsync(course.CourseId), Times.Once);
        _routingClientMock.Verify(r => r.ExportGraphAsync(course.CourseCode, "interaction", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExportCourseGraphAsync_NonExistingCourse_ThrowsCourseNotFoundException()
    {
        _courseRepoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Course?)null);

        await _sut.Invoking(s => s.ExportCourseGraphAsync(999))
            .Should().ThrowAsync<CourseNotFoundException>();

        _routingClientMock.Verify(r => r.ExportGraphAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdatePostAsync_OwnPost_UpdatesAndReturnsPost()
    {
        var post = TestDataFactory.PostFaker.Generate();
        post.PostId = 1;
        post.UserId = 10;
        post.Community = new Community { CourseId = 1 };
        Post? capturedPost = null;

        _postRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Post>>())).ReturnsAsync(post);
        _postRepoMock.Setup(r => r.Update(It.IsAny<Post>())).Callback<Post>(p => capturedPost = p);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        var result = await _sut.UpdatePostAsync(1, 10, "Updated content");

        result.Should().NotBeNull();
        result.Content.Should().Be("Updated content");
        result.PostId.Should().Be(1);

        capturedPost.Should().NotBeNull();
        capturedPost!.Content.Should().Be("Updated content");
        capturedPost.PostId.Should().Be(1);

        _postRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<Post>>()), Times.Once);
        _postRepoMock.Verify(r => r.Update(post), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task UpdatePostAsync_NonExistingPost_ThrowsPostNotFoundException()
    {
        _postRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Post>>())).ReturnsAsync((Post?)null);

        await _sut.Invoking(s => s.UpdatePostAsync(999, 1, "content"))
            .Should().ThrowAsync<PostNotFoundException>();

        _postRepoMock.Verify(r => r.Update(It.IsAny<Post>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task UpdatePostAsync_NotOwnPost_ThrowsUnauthorizedAccessException()
    {
        var post = TestDataFactory.PostFaker.Generate();
        post.PostId = 1;
        post.UserId = 10;
        post.Community = new Community { CourseId = 1 };

        _postRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Post>>())).ReturnsAsync(post);

        await _sut.Invoking(s => s.UpdatePostAsync(1, 99, "content"))
            .Should().ThrowAsync<UnauthorizedAccessException>();

        _postRepoMock.Verify(r => r.Update(It.IsAny<Post>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task DeletePostAsync_OwnPost_DeletesPost()
    {
        var post = TestDataFactory.PostFaker.Generate();
        post.PostId = 1;
        post.UserId = 10;
        post.Community = new Community { CourseId = 1 };

        _postRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Post>>())).ReturnsAsync(post);

        await _sut.DeletePostAsync(1, 10);

        _postRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<Post>>()), Times.Once);
        _postRepoMock.Verify(r => r.Delete(post), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
        _classRepoMock.Verify(r => r.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Class, bool>>>()), Times.Never);
    }

    [Fact]
    public async Task DeletePostAsync_NonExistingPost_ThrowsPostNotFoundException()
    {
        _postRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Post>>())).ReturnsAsync((Post?)null);

        await _sut.Invoking(s => s.DeletePostAsync(999, 1))
            .Should().ThrowAsync<PostNotFoundException>();

        _postRepoMock.Verify(r => r.Delete(It.IsAny<Post>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task DeletePostAsync_InstructorCanDeleteOthersPost()
    {
        var post = TestDataFactory.PostFaker.Generate();
        post.PostId = 1;
        post.UserId = 10;
        post.Community = new Community { CourseId = 5 };

        _postRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Post>>())).ReturnsAsync(post);
        _classRepoMock.Setup(r => r.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Class, bool>>>())).ReturnsAsync(true);

        await _sut.DeletePostAsync(1, 99);

        _postRepoMock.Verify(r => r.Delete(post), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
        _classRepoMock.Verify(r => r.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Class, bool>>>()), Times.Once);
    }

    [Fact]
    public async Task DeletePostAsync_NotOwnerAndNotInstructor_ThrowsUnauthorizedAccessException()
    {
        var post = TestDataFactory.PostFaker.Generate();
        post.PostId = 1;
        post.UserId = 10;
        post.Community = new Community { CourseId = 5 };

        _postRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Post>>())).ReturnsAsync(post);
        _classRepoMock.Setup(r => r.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Class, bool>>>())).ReturnsAsync(false);

        await _sut.Invoking(s => s.DeletePostAsync(1, 99))
            .Should().ThrowAsync<UnauthorizedAccessException>();

        _postRepoMock.Verify(r => r.Delete(It.IsAny<Post>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task AddCommentAsync_ExistingPost_AddsAndReturnsComment()
    {
        var post = TestDataFactory.PostFaker.Generate();
        post.PostId = 1;
        Comment? capturedComment = null;

        _postRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(post);
        _commentRepoMock.Setup(r => r.Add(It.IsAny<Comment>())).Callback<Comment>(c => capturedComment = c);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        var result = await _sut.AddCommentAsync(1, 10, "Great answer!");

        result.Should().NotBeNull();
        result.Content.Should().Be("Great answer!");
        result.UserId.Should().Be(10);
        result.PostId.Should().Be(1);

        capturedComment.Should().NotBeNull();
        capturedComment!.Content.Should().Be("Great answer!");
        capturedComment.UserId.Should().Be(10);
        capturedComment.PostId.Should().Be(1);

        _postRepoMock.Verify(r => r.GetByIdAsync(1), Times.Once);
        _commentRepoMock.Verify(r => r.Add(It.IsAny<Comment>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task AddCommentAsync_NonExistingPost_ThrowsPostNotFoundException()
    {
        _postRepoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Post?)null);

        await _sut.Invoking(s => s.AddCommentAsync(999, 1, "content"))
            .Should().ThrowAsync<PostNotFoundException>();

        _commentRepoMock.Verify(r => r.Add(It.IsAny<Comment>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task ToggleUpvoteAsync_NewUpvote_AddsVoteAndReturnsTrue()
    {
        var post = new Post { PostId = 1, Content = "test", UserId = 1 };
        var user = new User { UserId = 10, FullName = "Test", Email = "test@test.com" };
        PostVote? capturedVote = null;

        _postRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(post);
        _userRepoMock.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(user);
        _voteRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<PostVote>>())).ReturnsAsync((PostVote?)null);
        _voteRepoMock.Setup(r => r.Add(It.IsAny<PostVote>())).Callback<PostVote>(v => capturedVote = v);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        var result = await _sut.ToggleUpvoteAsync(1, 10);

        result.Should().BeTrue();

        capturedVote.Should().NotBeNull();
        capturedVote!.PostId.Should().Be(1);
        capturedVote.UserId.Should().Be(10);

        _voteRepoMock.Verify(r => r.Add(It.IsAny<PostVote>()), Times.Once);
        _voteRepoMock.Verify(r => r.Delete(It.IsAny<PostVote>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task ToggleUpvoteAsync_ExistingUpvote_RemovesVoteAndReturnsFalse()
    {
        var post = new Post { PostId = 1, Content = "test", UserId = 1 };
        var user = new User { UserId = 10, FullName = "Test", Email = "test@test.com" };
        var existingVote = new PostVote { PostVoteId = 1, PostId = 1, UserId = 10 };

        _postRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(post);
        _userRepoMock.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(user);
        _voteRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<PostVote>>())).ReturnsAsync(existingVote);
        _voteRepoMock.Setup(r => r.Delete(existingVote));
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        var result = await _sut.ToggleUpvoteAsync(1, 10);

        result.Should().BeFalse();

        _voteRepoMock.Verify(r => r.Delete(existingVote), Times.Once);
        _voteRepoMock.Verify(r => r.Add(It.IsAny<PostVote>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task ToggleUpvoteAsync_NonExistingPost_ThrowsPostNotFoundException()
    {
        _postRepoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Post?)null);

        await _sut.Invoking(s => s.ToggleUpvoteAsync(999, 1))
            .Should().ThrowAsync<PostNotFoundException>();

        _userRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<int>()), Times.Never);
        _voteRepoMock.Verify(r => r.Add(It.IsAny<PostVote>()), Times.Never);
        _voteRepoMock.Verify(r => r.Delete(It.IsAny<PostVote>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task ToggleUpvoteAsync_NonExistingUser_ThrowsUserNotFoundException()
    {
        var post = new Post { PostId = 1, Content = "test", UserId = 1 };

        _postRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(post);
        _userRepoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((User?)null);

        await _sut.Invoking(s => s.ToggleUpvoteAsync(1, 999))
            .Should().ThrowAsync<UserNotFoundException>();

        _voteRepoMock.Verify(r => r.Add(It.IsAny<PostVote>()), Times.Never);
        _voteRepoMock.Verify(r => r.Delete(It.IsAny<PostVote>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task DeleteCommentAsync_OwnComment_DeletesComment()
    {
        var comment = new Comment { CommentId = 1, UserId = 10, Content = "test" };

        _commentRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(comment);

        await _sut.DeleteCommentAsync(1, 10);

        _commentRepoMock.Verify(r => r.GetByIdAsync(1), Times.Once);
        _commentRepoMock.Verify(r => r.Delete(comment), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task DeleteCommentAsync_NonExistingComment_ThrowsCommentNotFoundException()
    {
        _commentRepoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Comment?)null);

        await _sut.Invoking(s => s.DeleteCommentAsync(999, 1))
            .Should().ThrowAsync<CommentNotFoundException>();

        _commentRepoMock.Verify(r => r.Delete(It.IsAny<Comment>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task DeleteCommentAsync_NotOwnComment_ThrowsUnauthorizedAccessException()
    {
        var comment = new Comment { CommentId = 1, UserId = 10, Content = "test" };

        _commentRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(comment);

        await _sut.Invoking(s => s.DeleteCommentAsync(1, 99))
            .Should().ThrowAsync<UnauthorizedAccessException>();

        _commentRepoMock.Verify(r => r.Delete(It.IsAny<Comment>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task GetCourseInstructorRolesAsync_ReturnsMappedRoles()
    {
        int courseId = 1;
        var instructor = TestDataFactory.InstructorFaker.Generate();
        instructor.UserId = 100;
        instructor.InstructorRole = InstructorRole.AssociateProfessor;

        var classes = new List<Class>
        {
            new Class
            {
                ClassId = 1,
                CourseId = courseId,
                InstructorId = 100,
                Instructor = instructor
            }
        };

        _classRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<Class>>())).ReturnsAsync(classes);

        var result = await _sut.GetCourseInstructorRolesAsync(courseId, new[] { 100 });

        result.Should().ContainKey(100);
        result[100].Should().Be("Assoc. Professor");

        _classRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<Class>>()), Times.Once);
    }

    [Fact]
    public async Task GetCourseInstructorRolesAsync_MultipleInstructors_ReturnsAllRoles()
    {
        int courseId = 1;
        var instructor1 = TestDataFactory.InstructorFaker.Generate();
        instructor1.UserId = 100;
        instructor1.InstructorRole = InstructorRole.Professor;

        var instructor2 = TestDataFactory.InstructorFaker.Generate();
        instructor2.UserId = 200;
        instructor2.InstructorRole = InstructorRole.TeachingAssistant;

        var classes = new List<Class>
        {
            new() { ClassId = 1, CourseId = courseId, InstructorId = 100, Instructor = instructor1 },
            new() { ClassId = 2, CourseId = courseId, InstructorId = 200, Instructor = instructor2 }
        };

        _classRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<Class>>())).ReturnsAsync(classes);

        var result = await _sut.GetCourseInstructorRolesAsync(courseId, new[] { 100, 200 });

        result.Should().HaveCount(2);
        result[100].Should().Be("Professor");
        result[200].Should().Be("TA");

        _classRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<Class>>()), Times.Once);
    }

    [Fact]
    public async Task GetCourseInstructorRolesAsync_EmptyUserIds_ReturnsEmptyDictionary()
    {
        var result = await _sut.GetCourseInstructorRolesAsync(1, Enumerable.Empty<int>());

        result.Should().BeEmpty();
        _classRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<Class>>()), Times.Never);
    }

    [Fact]
    public async Task GetCoursePostsAsync_ExistingCommunity_ReturnsPosts()
    {
        var course = TestDataFactory.CourseFaker.Generate();
        var community = new Community { CommunityId = 1, CourseId = course.CourseId };
        var posts = new List<Post>
        {
            new Post { PostId = 1, Content = "Post 1", CommunityId = community.CommunityId, UserId = 1 },
            new Post { PostId = 2, Content = "Post 2", CommunityId = community.CommunityId, UserId = 2 },
        };

        _communityRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Community>>())).ReturnsAsync(community);
        _postRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<Post>>())).ReturnsAsync(posts);

        var result = await _sut.GetCoursePostsAsync(course.CourseId);

        result.Should().HaveCount(2);

        _communityRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<Community>>()), Times.Once);
        _postRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<Post>>()), Times.Once);
    }

    [Fact]
    public async Task GetCoursePostsAsync_Paginated_NoCommunity_ThrowsCourseNotFoundException()
    {
        int courseId = 999;
        var queryParams = new CommunityQueryParams { PageIndex = 1, PageSize = 10 };

        _communityRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Community>>())).ReturnsAsync((Community?)null);
        _courseRepoMock.Setup(r => r.GetByIdAsync(courseId)).ReturnsAsync((Course?)null);

        await _sut.Invoking(s => s.GetCoursePostsAsync(courseId, queryParams))
            .Should().ThrowAsync<CourseNotFoundException>();

        _postRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<Post>>()), Times.Never);
        _postRepoMock.Verify(r => r.CountAsync(It.IsAny<ISpecifications<Post>>()), Times.Never);
    }

    [Fact]
    public async Task GetQuestionPostAsync_ExistingPost_ReturnsPost()
    {
        var post = TestDataFactory.PostFaker.Generate();
        post.PostId = 1;
        post.Content = "Test question content";

        _postRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Post>>())).ReturnsAsync(post);

        var result = await _sut.GetQuestionPostAsync(1, 1);

        result.Should().NotBeNull();
        result.PostId.Should().Be(1);
        result.Content.Should().Be("Test question content");

        _postRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<Post>>()), Times.Once);
    }

    [Fact]
    public async Task CreateQuestionPostAsync_RoutingFails_StillReturnsPost()
    {
        var course = TestDataFactory.CourseFaker.Generate();
        var student = TestDataFactory.StudentFaker.Generate();
        var community = new Community { CommunityId = 1, CourseId = course.CourseId };

        var post = TestDataFactory.PostFaker.Generate();
        post.PostId = 1;
        post.Content = "Test question";

        _communityRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Community>>())).ReturnsAsync(community);
        _courseRepoMock.Setup(r => r.GetByIdAsync(course.CourseId)).ReturnsAsync(course);
        _postRepoMock.Setup(r => r.Add(It.IsAny<Post>()));
        _postRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync(post);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);
        _routingClientMock.Setup(r => r.RouteAsync(It.IsAny<QuestionRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Routing service unavailable"));

        var result = await _sut.CreateQuestionPostAsync(course.CourseId, student.UserId, "Test question");

        result.Should().NotBeNull();
        result.Content.Should().Be("Test question");

        _postRepoMock.Verify(r => r.Add(It.IsAny<Post>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.AtLeastOnce);
        _routingClientMock.Verify(r => r.RouteAsync(It.IsAny<QuestionRequest>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetCourseInstructorRolesAsync_NullInstructorId_SkipsEntry()
    {
        int courseId = 1;
        var classes = new List<Class>
        {
            new Class { ClassId = 1, CourseId = courseId, InstructorId = null, Instructor = null }
        };

        _classRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<Class>>())).ReturnsAsync(classes);

        var result = await _sut.GetCourseInstructorRolesAsync(courseId, new[] { 100 });

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetCourseInstructorRolesAsync_NullInstructor_SkipsEntry()
    {
        int courseId = 1;
        var classes = new List<Class>
        {
            new Class { ClassId = 1, CourseId = courseId, InstructorId = 100, Instructor = null }
        };

        _classRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<Class>>())).ReturnsAsync(classes);

        var result = await _sut.GetCourseInstructorRolesAsync(courseId, new[] { 100 });

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetCourseInstructorRolesAsync_DuplicateInstructorId_SkipsDuplicate()
    {
        int courseId = 1;
        var instructor = TestDataFactory.InstructorFaker.Generate();
        instructor.UserId = 100;
        instructor.InstructorRole = InstructorRole.Professor;

        var classes = new List<Class>
        {
            new Class { ClassId = 1, CourseId = courseId, InstructorId = 100, Instructor = instructor },
            new Class { ClassId = 2, CourseId = courseId, InstructorId = 100, Instructor = instructor }
        };

        _classRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<Class>>())).ReturnsAsync(classes);

        var result = await _sut.GetCourseInstructorRolesAsync(courseId, new[] { 100 });

        result.Should().HaveCount(1);
        result[100].Should().Be("Professor");
    }

    [Fact]
    public async Task GetCourseInstructorRolesAsync_UnknownRole_SkipsEntry()
    {
        int courseId = 1;
        var instructor = TestDataFactory.InstructorFaker.Generate();
        instructor.UserId = 100;
        instructor.InstructorRole = null;

        var classes = new List<Class>
        {
            new Class { ClassId = 1, CourseId = courseId, InstructorId = 100, Instructor = instructor }
        };

        _classRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<Class>>())).ReturnsAsync(classes);

        var result = await _sut.GetCourseInstructorRolesAsync(courseId, new[] { 100 });

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetCourseInstructorRolesAsync_AllRoleLabelsAreMapped()
    {
        int courseId = 1;
        var roles = new (InstructorRole Role, string Label)[]
        {
            (InstructorRole.TeachingAssistant, "TA"),
            (InstructorRole.Lecturer, "Lecturer"),
            (InstructorRole.AssistantLecturer, "Asst. Lecturer"),
            (InstructorRole.AssociateProfessor, "Assoc. Professor"),
            (InstructorRole.Professor, "Professor"),
        };

        var classes = roles.Select((r, i) =>
        {
            var instructor = TestDataFactory.InstructorFaker.Generate();
            instructor.UserId = 100 + i;
            instructor.InstructorRole = r.Role;
            return new Class
            {
                ClassId = i + 1,
                CourseId = courseId,
                InstructorId = 100 + i,
                Instructor = instructor
            };
        }).ToList();

        _classRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<Class>>())).ReturnsAsync(classes);

        var result = await _sut.GetCourseInstructorRolesAsync(courseId, Enumerable.Range(100, 5));

        foreach (var (_, label) in roles)
            result.Values.Should().Contain(label);
    }

    [Fact]
    public async Task CreateQuestionPostAsync_RoutingReturnsNull_StillReturnsPost()
    {
        var course = TestDataFactory.CourseFaker.Generate();
        var student = TestDataFactory.StudentFaker.Generate();
        var community = new Community { CommunityId = 1, CourseId = course.CourseId };

        _communityRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Community>>())).ReturnsAsync(community);
        _postRepoMock.Setup(r => r.Add(It.IsAny<Post>()));
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);
        _routingClientMock.Setup(r => r.RouteAsync(It.IsAny<QuestionRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((RoutingResponse?)null);

        var result = await _sut.CreateQuestionPostAsync(course.CourseId, student.UserId, "Test");

        result.Should().NotBeNull();
        result.Content.Should().Be("Test");

        _notificationServiceMock.Verify(n => n.SendToManyAsync(It.IsAny<IEnumerable<int>>(), It.IsAny<NotificationType>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>()), Times.Never);
        _postCandidateRepoMock.Verify(r => r.Add(It.IsAny<PostCandidate>()), Times.Never);
    }

    [Fact]
    public async Task CreateQuestionPostAsync_RoutingReturnsOnlyPostAuthor_SkipsNotifications()
    {
        var course = TestDataFactory.CourseFaker.Generate();
        var student = TestDataFactory.StudentFaker.Generate();
        var community = new Community { CommunityId = 1, CourseId = course.CourseId };
        var post = TestDataFactory.PostFaker.Generate();
        post.PostId = 1;
        post.UserId = student.UserId;

        _communityRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Community>>())).ReturnsAsync(community);
        _postRepoMock.Setup(r => r.Add(It.IsAny<Post>()));
        _postRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync(post);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);
        _routingClientMock.Setup(r => r.RouteAsync(It.IsAny<QuestionRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RoutingResponse("branch", null, new List<RankedCandidate>
            {
                new(post.UserId.ToString(), 0.95, new Dictionary<string, object>())
            }));

        var result = await _sut.CreateQuestionPostAsync(course.CourseId, student.UserId, "Test");

        result.Should().NotBeNull();
        _notificationServiceMock.Verify(n => n.SendToManyAsync(It.IsAny<IEnumerable<int>>(), It.IsAny<NotificationType>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>()), Times.Never);
        _postCandidateRepoMock.Verify(r => r.Add(It.IsAny<PostCandidate>()), Times.Never);
    }
}
