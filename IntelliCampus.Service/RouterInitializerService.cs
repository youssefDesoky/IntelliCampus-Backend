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

            var courses = await uow.GetRepository<Course, int>().GetAllAsync(
                new CourseSpec());

            foreach (var course in courses)
            {
                if (cancellationToken.IsCancellationRequested) break;

                if (course.StudentCourses.Count == 0)
                    continue;

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
                        .GetAllAsync(new CommunityPostSpec(community.CommunityId));

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

                var request = new InitializeRequest(
                    CourseId: courseCode,
                    PrereqEdges: prereqEdges.Count > 0 ? prereqEdges : null,
                    ArchivedQuestions: archivedQuestions,
                    Interactions: interactions,
                    Answers: answers,
                    Students: students
                );

                try
                {
                    await routingClient.InitializeAsync(request, cancellationToken);
                    _logger.LogInformation("Router initialized for course {Code} (id={Id})",
                        course.CourseCode, course.CourseId);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex,
                        "Router initialization skipped for course {Code} (service unavailable)",
                        course.CourseCode);
                }
            }

            _logger.LogInformation("Router initializer completed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Router initializer failed");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

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
}
