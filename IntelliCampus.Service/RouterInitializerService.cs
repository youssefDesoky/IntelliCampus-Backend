using IntelliCampus.Domain.Entities;
using IntelliCampus.Domain.Interfaces;
using IntelliCampus.Service.Specifications;
using IntelliCampus.Service_Abstraction;
using IntelliCampus.Shared.Dtos.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace IntelliCampus.Service;

public class RouterInitializerService : IHostedService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<RouterInitializerService> _logger;

    public RouterInitializerService(
        IServiceScopeFactory scopeFactory,
        ILogger<RouterInitializerService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Router initializer starting...");

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var routingClient = scope.ServiceProvider.GetRequiredService<IRoutingClientService>();

            var (courses, allStudentCourses, allGrades, allPrereqs) = await LoadRouterDataAsync(uow);

            var (studentCourseLookup, gradeLookup, prereqLookup) = BuildRouterLookups(
                allStudentCourses, allGrades, allPrereqs);

            var requests = new List<(string CourseCode, int CourseId, InitializeRequest Request)>();

            foreach (var course in courses)
            {
                if (cancellationToken.IsCancellationRequested) break;

                var request = await BuildCourseRequestAsync(
                    course, studentCourseLookup, gradeLookup, prereqLookup, uow, _logger, cancellationToken);

                if (request is not null)
                    requests.Add((course.CourseCode ?? course.CourseId.ToString(), course.CourseId, request));
            }

            _logger.LogInformation("Initializing {Count} course routers in parallel...", requests.Count);

            await InitializeRoutersInParallelAsync(requests, routingClient, _logger, cancellationToken);

            _logger.LogInformation("Router initializer completed — {Count} courses initialized", requests.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Router initializer failed");
            throw;
        }
    }

    private async Task<(
        List<Course> Courses,
        List<StudentCourse> AllStudentCourses,
        List<Grade> AllGrades,
        List<CoursePrerequisite> AllPrereqs
    )> LoadRouterDataAsync(IUnitOfWork uow)
    {
        var courses = (await uow.GetRepository<Course, int>()
            .GetAllAsync(new CourseRouterSpec(), asNoTracking: true)).ToList();
        var allStudentCourses = (await uow.GetRepository<StudentCourse, (int, int)>()
            .GetAllAsync(new StudentCourseWithStudentSpec(), asNoTracking: true)).ToList();
        var allGrades = (await uow.GetRepository<Grade, int>()
            .GetAllAsync(specifications: null, asNoTracking: true)).ToList();
        var allPrereqs = (await uow.GetRepository<CoursePrerequisite, int>()
            .GetAllAsync(new CoursePrerequisiteWithCourseSpec(), asNoTracking: true)).ToList();

        return (courses, allStudentCourses, allGrades, allPrereqs);
    }

    private static (
        Dictionary<int, List<StudentCourse>> StudentCourseLookup,
        Dictionary<int, List<Grade>> GradeLookup,
        Dictionary<int, List<CoursePrerequisite>> PrereqLookup
    ) BuildRouterLookups(
        List<StudentCourse> allStudentCourses,
        List<Grade> allGrades,
        List<CoursePrerequisite> allPrereqs)
    {
        var studentCourseLookup = allStudentCourses
            .GroupBy(sc => sc.CourseId)
            .ToDictionary(g => g.Key, g => g.ToList());
        var gradeLookup = allGrades
            .GroupBy(g => g.CourseId)
            .ToDictionary(g => g.Key, g => g.ToList());
        var prereqLookup = allPrereqs
            .GroupBy(p => p.CourseId)
            .ToDictionary(g => g.Key, g => g.ToList());

        return (studentCourseLookup, gradeLookup, prereqLookup);
    }

    private async Task<InitializeRequest?> BuildCourseRequestAsync(
        Course course,
        Dictionary<int, List<StudentCourse>> studentCourseLookup,
        Dictionary<int, List<Grade>> gradeLookup,
        Dictionary<int, List<CoursePrerequisite>> prereqLookup,
        IUnitOfWork uow,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        var courseStudentCourses = studentCourseLookup.GetValueOrDefault(course.CourseId, []);
        if (courseStudentCourses.Count == 0)
            return null;

        var courseCode = course.CourseCode ?? course.CourseId.ToString();

        var coursePrereqs = prereqLookup.GetValueOrDefault(course.CourseId, []);
        var prereqEdges = coursePrereqs
            .Select(p => new List<string>
            {
                p.PrerequisiteCourse.CourseCode ?? p.PrerequisiteCourseId.ToString(),
                courseCode,
            })
            .ToList();

        var courseGrades = gradeLookup.GetValueOrDefault(course.CourseId, []);
        var students = courseStudentCourses
            .Select(sc => new StudentData(
                StudentId: sc.Student.UserId.ToString(),
                Name: sc.Student.User.FullName,
                Performance: CalculatePerformance(sc.Student, courseGrades),
                CompletedTopics: new List<string> {
                    courseCode
                }
            ))
            .DistinctBy(s => s.StudentId)
            .ToList();

        var community = await uow.GetRepository<Community, int>()
            .GetByIdAsync(new CommunityByCourseSpec(course.CourseId));

        var archivedQuestions = new List<QuestionRequest>();
        var interactions = new List<InteractionData>();
        var answers = new List<AnswerData>();

        if (community is not null)
        {
            var posts = await uow.GetRepository<Post, int>()
                .GetAllAsync(new CommunityPostSpec(community.CommunityId), asNoTracking: true);

            foreach (var post in posts)
            {
                archivedQuestions.Add(new QuestionRequest(
                    QuestionId: post.PostId.ToString(),
                    Text: post.Content,
                    CourseId: courseCode
                ));

                interactions.Add(new InteractionData(
                    StudentId: post.UserId.ToString(),
                    CourseId: courseCode,
                    Action: "comment"
                ));

                foreach (var comment in post.Comments)
                {
                    answers.Add(new AnswerData(
                        AnswerId: comment.CommentId.ToString(),
                        QuestionId: post.PostId.ToString(),
                        AnswererId: comment.UserId.ToString(),
                        Upvotes: 0,
                        Accepted: false
                    ));

                    interactions.Add(new InteractionData(
                        StudentId: comment.UserId.ToString(),
                        CourseId: courseCode,
                        Action: "comment"
                    ));
                }
            }
        }

        if (archivedQuestions.Count == 0)
        {
            logger.LogDebug("Skipping router init for course {Code} — no community posts", courseCode);
            return null;
        }

        return new InitializeRequest(
            CourseId: courseCode,
            PrereqEdges: prereqEdges.Count > 0 ? prereqEdges : null,
            ArchivedQuestions: archivedQuestions,
            Interactions: interactions,
            Answers: answers,
            Students: students
        );
    }

    private async Task InitializeRoutersInParallelAsync(
        List<(string CourseCode, int CourseId, InitializeRequest Request)> requests,
        IRoutingClientService routingClient,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        await Parallel.ForEachAsync(
            requests,
            new ParallelOptions { MaxDegreeOfParallelism = 5, CancellationToken = cancellationToken },
            async (item, ct) =>
            {
                try
                {
                    await routingClient.InitializeAsync(item.Request, ct);
                    logger.LogInformation("Router initialized for course {Code} (id={Id})",
                        item.CourseCode, item.CourseId);
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex,
                        "Router initialization skipped for course {Code} (service unavailable)",
                        item.CourseCode);
                }
            });
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private static double CalculatePerformance(Student student,
        List<Grade> courseGrades)
    {
        var studentGrades = courseGrades
            .Where(g => g.StudentId == student.UserId && g.Status == "Graded")
            .ToList();

        if (studentGrades.Count == 0)
            return 0.5;

        var weightedSum = studentGrades.Sum(g => (double)(g.Score * g.Weight / 100));
        return Math.Clamp(weightedSum / 100.0, 0.0, 1.0);
    }
}
