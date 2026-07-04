using System.Net;
using IntelliCampus.Domain.Entities;
using IntelliCampus.IntegrationTests.Helpers;
using IntelliCampus.Presistence.Data.Contexts;
using IntelliCampus.Service_Abstraction;
using IntelliCampus.Shared.Dtos.Routing;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace IntelliCampus.IntegrationTests.Tests;

public class AiServiceTimeoutTests : IDisposable
{
    private readonly CustomWebApplicationFactory _factory = new();

    private (int courseId, int postId) SeedTestData()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IntelliCampusDbContext>();

        var course = db.Courses.FirstOrDefault();
        if (course != null)
        {
            var post = db.Posts.First();
            return (course.CourseId, post.PostId);
        }

        var user = new User
        {
            NationalId = "TEST123",
            FullName = "Test User",
            Email = "test@test.com",
            Password = "dummy"
        };
        db.Users.Add(user);
        db.SaveChanges();

        course = new Course
        {
            CourseCode = "CS101",
            CourseName = "Test Course",
            CreditHours = 3
        };
        db.Courses.Add(course);
        db.SaveChanges();

        var community = new Community { CourseId = course.CourseId };
        db.Communities.Add(community);
        db.SaveChanges();

        var newPost = new Post
        {
            Content = "Test question?",
            UserId = user.UserId,
            CommunityId = community.CommunityId,
            CreatedAt = DateTime.UtcNow
        };
        db.Posts.Add(newPost);
        db.SaveChanges();

        return (course.CourseId, newPost.PostId);
    }

    [Fact]
    public async Task Test08_AI_Routing_Timeout_Returns_500()
    {
        var (courseId, postId) = SeedTestData();
        _factory.RoutingClientMock.Reset();
        _factory.RoutingClientMock
            .Setup(m => m.RouteAsync(It.IsAny<QuestionRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new TaskCanceledException("The request was canceled due to timeout"));

        var client = _factory.CreateClientWithToken(JwtTokenHelper.StudentToken);
        var response = await client.PostAsync($"/api/courses/{courseId}/community/route",
            TestHelper.ToJsonContent(new { postId, topN = 3 }));

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
    }

    [Fact]
    public async Task Test09_AI_Routing_503_Returns_503()
    {
        var (courseId, postId) = SeedTestData();
        _factory.RoutingClientMock.Reset();
        _factory.RoutingClientMock
            .Setup(m => m.RouteAsync(It.IsAny<QuestionRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new RouterNotInitializedException("CS101"));

        var client = _factory.CreateClientWithToken(JwtTokenHelper.StudentToken);
        var response = await client.PostAsync($"/api/courses/{courseId}/community/route",
            TestHelper.ToJsonContent(new { postId, topN = 3 }));

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
    }

    [Fact]
    public async Task Test10_AI_Routing_HttpRequestException_Returns_500()
    {
        var (courseId, postId) = SeedTestData();
        _factory.RoutingClientMock.Reset();
        _factory.RoutingClientMock
            .Setup(m => m.RouteAsync(It.IsAny<QuestionRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Connection refused"));

        var client = _factory.CreateClientWithToken(JwtTokenHelper.StudentToken);
        var response = await client.PostAsync($"/api/courses/{courseId}/community/route",
            TestHelper.ToJsonContent(new { postId, topN = 3 }));

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
    }

    public void Dispose() => _factory.Dispose();
}
