using FluentAssertions;
using IntelliCampus.Domain.Entities;
using IntelliCampus.Domain.Interfaces;
using IntelliCampus.Service;
using IntelliCampus.Service_Abstraction;
using IntelliCampus.Shared.Dtos.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;

namespace IntelliCampus.UnitTests.Services;

public class RouterInitializerServiceTests
{
    private readonly Mock<IServiceScopeFactory> _scopeFactoryMock;
    private readonly Mock<ILogger<RouterInitializerService>> _loggerMock;
    private readonly RouterInitializerService _sut;

    public RouterInitializerServiceTests()
    {
        _scopeFactoryMock = new Mock<IServiceScopeFactory>();
        _loggerMock = new Mock<ILogger<RouterInitializerService>>();

        _sut = new RouterInitializerService(_scopeFactoryMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task StartAsync_CreatesScopeAndResolvesServices()
    {
        var (scopeMock, uowMock, routingClientMock) = SetupBaseMocks(out var courseRepoMock);
        courseRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<Course>>())).ReturnsAsync([]);

        await _sut.StartAsync(CancellationToken.None);

        _scopeFactoryMock.Verify(f => f.CreateScope(), Times.Once);
    }

    [Fact]
    public async Task StopAsync_Completes()
    {
        await _sut.StopAsync(CancellationToken.None);
    }

    [Fact]
    public async Task StartAsync_NoCourses_DoesNotInitializeAnyRouter()
    {
        var (scopeMock, uowMock, routingClientMock) = SetupBaseMocks(out var courseRepoMock);
        courseRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<Course>>())).ReturnsAsync([]);

        await _sut.StartAsync(CancellationToken.None);

        routingClientMock.Verify(r => r.InitializeAsync(It.IsAny<InitializeRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task StartAsync_CourseWithNoStudentCourses_SkipsCourse()
    {
        var (scopeMock, uowMock, routingClientMock) = SetupBaseMocks(out var courseRepoMock);
        var studentCourseRepoMock = new Mock<IGenericRepository<StudentCourse, int>>();
        var gradeRepoMock = new Mock<IGenericRepository<Grade, int>>();
        var prereqRepoMock = new Mock<IGenericRepository<CoursePrerequisite, int>>();

        uowMock.Setup(u => u.GetRepository<StudentCourse, int>()).Returns(studentCourseRepoMock.Object);
        uowMock.Setup(u => u.GetRepository<Grade, int>()).Returns(gradeRepoMock.Object);
        uowMock.Setup(u => u.GetRepository<CoursePrerequisite, int>()).Returns(prereqRepoMock.Object);

        var course = new Course { CourseId = 1, CourseCode = "CS101", CourseName = "CS 101" };
        courseRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<Course>>())).ReturnsAsync([course]);
        studentCourseRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<StudentCourse>>())).ReturnsAsync([]);
        gradeRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync([]);
        prereqRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<CoursePrerequisite>>())).ReturnsAsync([]);

        await _sut.StartAsync(CancellationToken.None);

        routingClientMock.Verify(r => r.InitializeAsync(It.IsAny<InitializeRequest>(), It.IsAny<CancellationToken>()), Times.Never);
        studentCourseRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<StudentCourse>>()), Times.Once);
        gradeRepoMock.Verify(r => r.GetAllAsync(), Times.Once);
        prereqRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<CoursePrerequisite>>()), Times.Once);
    }

    [Fact]
    public async Task StartAsync_CourseWithNullCode_UsesCourseIdAsCode()
    {
        var (scopeMock, uowMock, routingClientMock) = SetupBaseMocks(out var courseRepoMock);
        var studentCourseRepoMock = new Mock<IGenericRepository<StudentCourse, int>>();
        var gradeRepoMock = new Mock<IGenericRepository<Grade, int>>();
        var prereqRepoMock = new Mock<IGenericRepository<CoursePrerequisite, int>>();
        var communityRepoMock = new Mock<IGenericRepository<Community, int>>();
        var postRepoMock = new Mock<IGenericRepository<Post, int>>();

        uowMock.Setup(u => u.GetRepository<StudentCourse, int>()).Returns(studentCourseRepoMock.Object);
        uowMock.Setup(u => u.GetRepository<Grade, int>()).Returns(gradeRepoMock.Object);
        uowMock.Setup(u => u.GetRepository<CoursePrerequisite, int>()).Returns(prereqRepoMock.Object);
        uowMock.Setup(u => u.GetRepository<Community, int>()).Returns(communityRepoMock.Object);
        uowMock.Setup(u => u.GetRepository<Post, int>()).Returns(postRepoMock.Object);

        var course = new Course { CourseId = 5, CourseCode = null, CourseName = "No Code" };
        courseRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<Course>>())).ReturnsAsync([course]);

        var student = new Student { UserId = 1, User = new User { FullName = "Test", NationalId = "NID", Email = "t@t.com", Password = "pwd" } };
        studentCourseRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<StudentCourse>>()))
            .ReturnsAsync([new StudentCourse { CourseId = 5, Student = student }]);
        gradeRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<Grade>>())).ReturnsAsync([]);
        prereqRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<CoursePrerequisite>>())).ReturnsAsync([]);

        communityRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Community>>())).ReturnsAsync((Community?)null);

        await _sut.StartAsync(CancellationToken.None);

        routingClientMock.Verify(r => r.InitializeAsync(It.IsAny<InitializeRequest>(), It.IsAny<CancellationToken>()), Times.Never);
        communityRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<Community>>()), Times.Once);
    }

    [Fact]
    public async Task StartAsync_InitializeThrowsHttpException_CatchesAndLogsWarning()
    {
        var (scopeMock, uowMock, routingClientMock) = SetupBaseMocks(out var courseRepoMock);
        var studentCourseRepoMock = new Mock<IGenericRepository<StudentCourse, int>>();
        var gradeRepoMock = new Mock<IGenericRepository<Grade, int>>();
        var prereqRepoMock = new Mock<IGenericRepository<CoursePrerequisite, int>>();
        var communityRepoMock = new Mock<IGenericRepository<Community, int>>();
        var postRepoMock = new Mock<IGenericRepository<Post, int>>();

        uowMock.Setup(u => u.GetRepository<StudentCourse, int>()).Returns(studentCourseRepoMock.Object);
        uowMock.Setup(u => u.GetRepository<Grade, int>()).Returns(gradeRepoMock.Object);
        uowMock.Setup(u => u.GetRepository<CoursePrerequisite, int>()).Returns(prereqRepoMock.Object);
        uowMock.Setup(u => u.GetRepository<Community, int>()).Returns(communityRepoMock.Object);
        uowMock.Setup(u => u.GetRepository<Post, int>()).Returns(postRepoMock.Object);

        var course = new Course { CourseId = 1, CourseCode = "CS101", CourseName = "CS 101" };
        courseRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<Course>>())).ReturnsAsync([course]);

        var student = new Student { UserId = 1, User = new User { FullName = "Test", NationalId = "NID", Email = "t@t.com", Password = "pwd" } };
        studentCourseRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<StudentCourse>>()))
            .ReturnsAsync([new StudentCourse { CourseId = 1, Student = student }]);
        gradeRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<Grade>>())).ReturnsAsync([]);
        prereqRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<CoursePrerequisite>>())).ReturnsAsync([]);

        var community = new Community { CommunityId = 1, CourseId = 1 };
        communityRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Community>>())).ReturnsAsync(community);
        postRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<Post>>()))
            .ReturnsAsync([new Post { PostId = 1, Content = "Help?", UserId = 1, Comments = [] }]);

        routingClientMock.Setup(r => r.InitializeAsync(It.IsAny<InitializeRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Service unavailable"));

        await _sut.Invoking(s => s.StartAsync(CancellationToken.None)).Should().NotThrowAsync();

        routingClientMock.Verify(r => r.InitializeAsync(It.IsAny<InitializeRequest>(), It.IsAny<CancellationToken>()), Times.Once);
        postRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<Post>>()), Times.Once);
    }

    [Fact]
    public async Task StartAsync_CancellationRequested_StopsProcessing()
    {
        var (scopeMock, uowMock, routingClientMock) = SetupBaseMocks(out var courseRepoMock);
        var studentCourseRepoMock = new Mock<IGenericRepository<StudentCourse, int>>();
        var gradeRepoMock = new Mock<IGenericRepository<Grade, int>>();
        var prereqRepoMock = new Mock<IGenericRepository<CoursePrerequisite, int>>();

        uowMock.Setup(u => u.GetRepository<StudentCourse, int>()).Returns(studentCourseRepoMock.Object);
        uowMock.Setup(u => u.GetRepository<Grade, int>()).Returns(gradeRepoMock.Object);
        uowMock.Setup(u => u.GetRepository<CoursePrerequisite, int>()).Returns(prereqRepoMock.Object);

        var courses = Enumerable.Range(1, 10).Select(i => new Course
        {
            CourseId = i,
            CourseCode = $"CS{i:D3}",
            CourseName = $"Course {i}"
        }).ToList();

        courseRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<Course>>())).ReturnsAsync(courses);

        var student = new Student { UserId = 1, User = new User { FullName = "Test", NationalId = "NID", Email = "t@t.com", Password = "pwd" } };
        studentCourseRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<StudentCourse>>()))
            .ReturnsAsync([new StudentCourse { CourseId = 1, Student = student }]);
        gradeRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<Grade>>())).ReturnsAsync([]);
        prereqRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<CoursePrerequisite>>())).ReturnsAsync([]);

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await _sut.StartAsync(cts.Token);

        routingClientMock.Verify(r => r.InitializeAsync(It.IsAny<InitializeRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private (Mock<IServiceScope>, Mock<IUnitOfWork>, Mock<IRoutingClientService>) SetupBaseMocks(
        out Mock<IGenericRepository<Course, int>> courseRepoMock)
    {
        var scopeMock = new Mock<IServiceScope>();
        var serviceProviderMock = new Mock<IServiceProvider>();
        var uowMock = new Mock<IUnitOfWork>();
        var routingClientMock = new Mock<IRoutingClientService>();

        scopeMock.Setup(s => s.ServiceProvider).Returns(serviceProviderMock.Object);
        serviceProviderMock.Setup(p => p.GetService(typeof(IUnitOfWork))).Returns(uowMock.Object);
        serviceProviderMock.Setup(p => p.GetService(typeof(IRoutingClientService))).Returns(routingClientMock.Object);
        _scopeFactoryMock.Setup(f => f.CreateScope()).Returns(scopeMock.Object);

        courseRepoMock = new Mock<IGenericRepository<Course, int>>();
        uowMock.Setup(u => u.GetRepository<Course, int>()).Returns(courseRepoMock.Object);

        return (scopeMock, uowMock, routingClientMock);
    }
}
