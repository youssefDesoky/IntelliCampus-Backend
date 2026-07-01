using IntelliCampus.Domain.Entities;
using IntelliCampus.Domain.Entities.Enums;
using IntelliCampus.Domain.Interfaces;
using IntelliCampus.Service.Exceptions;
using IntelliCampus.Service.Specifications;
using IntelliCampus.Service_Abstraction;
using IntelliCampus.shared.Dtos.Attendance;
using IntelliCampus.Shared.Dtos.InstructorAnalytics;
using IntelliCampus.Shared.Params;

namespace IntelliCampus.Service;

public class InstructorAnalyticsService : IInstructorAnalyticsService
{
    private readonly ICourseService _courseService;
    private readonly IQuizService _quizService;
    private readonly IAssignmentService _assignmentService;
    private readonly ISessionService _sessionService;
    private readonly IClassService _classService;
    private readonly IUnitOfWork _unitOfWork;

    public InstructorAnalyticsService(
        ICourseService courseService,
        IQuizService quizService,
        IAssignmentService assignmentService,
        ISessionService sessionService,
        IClassService classService,
        IUnitOfWork unitOfWork)
    {
        _courseService = courseService;
        _quizService = quizService;
        _assignmentService = assignmentService;
        _sessionService = sessionService;
        _classService = classService;
        _unitOfWork = unitOfWork;
    }

    public async Task<CourseAnalyticsDto> GetCourseAnalyticsAsync(int courseId, int userId)
    {
        var course = await _courseService.GetByIdAsync(courseId);
        if (course is null)
            throw new CourseNotFoundException(courseId);

        var instructorId = await ResolveInstructorIdAsync(userId, courseId);

        var students = await _courseService.GetStudentsByCourseIdAsync(courseId);
        var totalStudents = students.Count();

        return new CourseAnalyticsDto
        {
            AssessmentPerformance = await BuildAssessmentPerformanceAsync(courseId, instructorId),
            SubmissionRate = await BuildSubmissionRateAsync(courseId, instructorId, totalStudents),
            WeeklyAttendance = await BuildWeeklyAttendanceAsync(courseId)
        };
    }

    private async Task<int> ResolveInstructorIdAsync(int userId, int courseId)
    {
        var instructorRepo = _unitOfWork.GetRepository<Instructor, int>();
        if (!await instructorRepo.AnyAsync(i => i.UserId == userId))
            throw new InstructorNotFoundException($"Instructor with user id {userId} not found");

        var instructorCourses = await _courseService.GetCoursesByInstructorIdAsync(new CourseQueryParams { InstructorId = userId, PageSize = 50 });
        if (!instructorCourses.Data.Any(c => c.CourseId == courseId))
            throw new ForbiddenException("Instructor is not assigned to this course");

        return userId;
    }

    private async Task<List<AssessmentPerformanceItemDto>> BuildAssessmentPerformanceAsync(int courseId, int instructorId)
    {
        var items = new List<AssessmentPerformanceItemDto>();

        var quizzes = await _quizService.GetByCourseIdAsync(courseId);
        var quizIds = quizzes.Select(q => q.Id).ToHashSet();
        var studentQuizRepo = _unitOfWork.GetRepository<StudentQuiz, int>();
        var allQuizResults = quizIds.Count > 0
            ? await studentQuizRepo.GetAllAsync(new StudentQuizSpec(quizIds, true), asNoTracking: true)
            : new List<StudentQuiz>();

        var quizScoresByQuiz = allQuizResults
            .Where(sq => sq.Score.HasValue)
            .GroupBy(sq => sq.QuizId)
            .ToDictionary(g => g.Key, g => g.ToList());

        foreach (var quiz in quizzes)
        {
            var scoredResults = quizScoresByQuiz.TryGetValue(quiz.Id, out var results)
                ? results
                : new List<StudentQuiz>();
            var avg = scoredResults.Count != 0
                ? Math.Round(scoredResults.Average(sq => (double)sq.Score!.Value), 1)
                : 0;

            items.Add(new AssessmentPerformanceItemDto
            {
                Name = quiz.Title,
                Average = avg,
                MaxScore = (double)quiz.MaxScore
            });
        }

        var assignments = await _assignmentService.GetByCourseIdAsync(courseId);
        var assignmentIds = assignments.Select(a => int.Parse(a.Id)).ToHashSet();
        var studentAssignmentRepo = _unitOfWork.GetRepository<StudentAssignment, int>();
        var allSubmissions = assignmentIds.Count > 0
            ? await studentAssignmentRepo.GetAllAsync(new StudentAssignmentSpec(assignmentIds, true), asNoTracking: true)
            : new List<StudentAssignment>();

        var subsByAssignment = allSubmissions
            .Where(sa => sa.Grade.HasValue)
            .GroupBy(sa => sa.AssignmentId)
            .ToDictionary(g => g.Key, g => g.ToList());

        foreach (var assignment in assignments)
        {
            var id = int.Parse(assignment.Id);
            var graded = subsByAssignment.TryGetValue(id, out var subs)
                ? subs
                : new List<StudentAssignment>();
            var avg = graded.Count != 0
                ? Math.Round(graded.Average(s => (double)s.Grade!.Value), 1)
                : 0;

            items.Add(new AssessmentPerformanceItemDto
            {
                Name = assignment.Title,
                Average = avg,
                MaxScore = (double)assignment.TotalPoints
            });
        }

        return items;
    }

    private async Task<List<SubmissionRateItemDto>> BuildSubmissionRateAsync(int courseId, int instructorId, int totalStudents)
    {
        if (totalStudents == 0)
            return
            [
                new SubmissionRateItemDto { Name = "Submitted", Value = 0, Color = "var(--color-bg-fill-success-default-light)" },
                new SubmissionRateItemDto { Name = "Not Submitted", Value = 100, Color = "var(--color-bg-fill-danger-default-light)" }
            ];

        var submittedStudentIds = new HashSet<int>();

        var quizzes = await _quizService.GetByCourseIdAsync(courseId);
        var quizIds = quizzes.Select(q => q.Id).ToHashSet();
        if (quizIds.Count > 0)
        {
            var studentQuizRepo = _unitOfWork.GetRepository<StudentQuiz, int>();
            foreach (var r in await studentQuizRepo.GetAllAsync(new StudentQuizSpec(quizIds, true), asNoTracking: true))
                submittedStudentIds.Add(r.StudentId);
        }

        var assignments = await _assignmentService.GetByCourseIdAsync(courseId);
        var assignmentIds = assignments.Select(a => int.Parse(a.Id)).ToHashSet();
        if (assignmentIds.Count > 0)
        {
            var studentAssignmentRepo = _unitOfWork.GetRepository<StudentAssignment, int>();
            foreach (var s in await studentAssignmentRepo.GetAllAsync(new StudentAssignmentSpec(assignmentIds, true), asNoTracking: true))
                submittedStudentIds.Add(s.StudentId);
        }

        var submittedPct = (int)Math.Round((double)submittedStudentIds.Count / totalStudents * 100);

        return
        [
            new SubmissionRateItemDto { Name = "Submitted", Value = submittedPct, Color = "var(--color-bg-fill-success-default-light)" },
            new SubmissionRateItemDto { Name = "Not Submitted", Value = 100 - submittedPct, Color = "var(--color-bg-fill-danger-default-light)" }
        ];
    }

    private async Task<List<WeeklyAttendanceItemDto>> BuildWeeklyAttendanceAsync(int courseId)
    {
        var classes = await _classService.GetByCourseIdAsync(courseId, new ClassQueryParams { PageSize = 50 });
        var classIds = classes.Select(c => c.ClassId).ToHashSet();

        if (classIds.Count == 0)
            return [];

        var sessionRepo = _unitOfWork.GetRepository<Session, int>();
        var sessions = await sessionRepo.GetAllAsync(new SessionSpec(classIds), asNoTracking: true);

        var allSessionDtos = sessions.Select(s => new SessionDto
        {
            SessionId = s.SessionId,
            Date = s.Date,
            ClassId = s.ClassId,
            Topic = s.Topic,
            TotalStudents = s.Attendances?.Count ?? 0,
            PresentCount = s.Attendances?.Count(a => a.Status == AttendanceStatus.Present) ?? 0
        }).ToList();

        if (allSessionDtos.Count == 0)
            return [];

        var ordered = allSessionDtos.OrderBy(s => s.Date).ToList();
        var earliest = ordered.FirstOrDefault()?.Date ?? default;

        return ordered
            .GroupBy(s => GetWeekLabel(s.Date, earliest))
            .Select(g => new WeeklyAttendanceItemDto
            {
                Week = g.Key,
                Present = g.Sum(s => s.PresentCount),
                Absent = g.Sum(s => s.TotalStudents - s.PresentCount),
                Excused = 0
            })
            .OrderBy(w => ParseWeekNumber(w.Week))
            .ToList();
    }

    private static string GetWeekLabel(DateTime date, DateTime earliest)
    {
        var diff = (date.Date - earliest.Date).Days;
        var weekNumber = (diff / 7) + 1;
        return $"W{weekNumber}";
    }

    private static int ParseWeekNumber(string week)
    {
        var digits = new string(week?.Skip(1).ToArray() ?? []);
        return int.TryParse(digits, out var n) ? n : int.MaxValue;
    }
}
