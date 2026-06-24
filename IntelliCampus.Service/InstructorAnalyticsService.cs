using IntelliCampus.Domain.Entities;
using IntelliCampus.Domain.Interfaces;
using IntelliCampus.Service.Exceptions;
using IntelliCampus.Service_Abstraction;
using IntelliCampus.Shared.Dtos.Export;
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
    private readonly IPdfExportService _pdfExportService;

    public InstructorAnalyticsService(
        ICourseService courseService,
        IQuizService quizService,
        IAssignmentService assignmentService,
        ISessionService sessionService,
        IClassService classService,
        IUnitOfWork unitOfWork,
        IPdfExportService pdfExportService)
    {
        _courseService = courseService;
        _quizService = quizService;
        _assignmentService = assignmentService;
        _sessionService = sessionService;
        _classService = classService;
        _unitOfWork = unitOfWork;
        _pdfExportService = pdfExportService;
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

    public async Task<byte[]> ExportCourseAnalyticsPdfAsync(int courseId, int userId)
    {
        var course = await _courseService.GetByIdAsync(courseId);
        if (course is null)
            throw new CourseNotFoundException(courseId);

        var instructorId = await ResolveInstructorIdAsync(userId, courseId);

        var instructorRepo = _unitOfWork.GetRepository<Instructor, int>();
        var instructors = await instructorRepo.GetAllAsync();
        var instructor = instructors.First(i => i.UserId == userId);

        var students = await _courseService.GetStudentsByCourseIdAsync(courseId);
        var totalStudents = students.Count();

        var dto = new CourseAnalyticsExportDto
        {
            CourseName = course.CourseName,
            CourseCode = course.CourseCode ?? "",
            InstructorName = instructor.FullName,
            AssessmentPerformance = await BuildAssessmentPerformanceAsync(courseId, instructorId),
            SubmissionRate = await BuildSubmissionRateAsync(courseId, instructorId, totalStudents),
            WeeklyAttendance = await BuildWeeklyAttendanceAsync(courseId)
        };

        return _pdfExportService.ExportCourseAnalytics(dto);
    }

    private async Task<int> ResolveInstructorIdAsync(int userId, int courseId)
    {
        var instructorRepo = _unitOfWork.GetRepository<Instructor, int>();
        var instructors = await instructorRepo.GetAllAsync();
        var instructor = instructors.FirstOrDefault(i => i.UserId == userId)
            ?? throw new InstructorNotFoundException($"Instructor with user id {userId} not found");

        var instructorCourses = await _courseService.GetCoursesByInstructorIdAsync(new CourseQueryParams { InstructorId = instructor.InstructorId });
        if (!instructorCourses.Any(c => c.CourseId == courseId))
            throw new ForbiddenException("Instructor is not assigned to this course");

        return instructor.InstructorId;
    }

    private async Task<List<AssessmentPerformanceItemDto>> BuildAssessmentPerformanceAsync(int courseId, int instructorId)
    {
        var items = new List<AssessmentPerformanceItemDto>();

        var quizzes = await _quizService.GetByCourseIdAsync(courseId);
        foreach (var quiz in quizzes)
        {
            var results = await _quizService.GetAllResultsAsync(quiz.Id, instructorId);
            var scoredResults = results.Where(r => r.Score.HasValue).ToList();
            var avg = scoredResults.Count != 0
                ? Math.Round(scoredResults.Average(r => (double)r.Score!.Value), 1)
                : 0;

            items.Add(new AssessmentPerformanceItemDto
            {
                Name = quiz.Title,
                Average = avg,
                MaxScore = (double)quiz.MaxScore
            });
        }

        var assignments = await _assignmentService.GetByCourseIdAsync(courseId);
        foreach (var assignment in assignments)
        {
            var submissions = await _assignmentService.GetAllSubmissionsAsync(int.Parse(assignment.Id), instructorId);
            var gradedSubmissions = submissions.Where(s => s.Grade is not null).ToList();
            var avg = gradedSubmissions.Count != 0
                ? Math.Round(gradedSubmissions.Average(s => (double)s.Grade!.Score), 1)
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

        foreach (var quiz in await _quizService.GetByCourseIdAsync(courseId))
        {
            try
            {
                foreach (var r in await _quizService.GetAllResultsAsync(quiz.Id, instructorId))
                    submittedStudentIds.Add(r.StudentId);
            }
            catch { }
        }

        foreach (var assignment in await _assignmentService.GetByCourseIdAsync(courseId))
        {
            try
            {
                foreach (var s in await _assignmentService.GetAllSubmissionsAsync(int.Parse(assignment.Id), instructorId))
                    submittedStudentIds.Add(s.StudentId);
            }
            catch { }
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
        var classes = await _classService.GetByCourseIdAsync(courseId, new ClassQueryParams());

        var allSessions = new List<IntelliCampus.shared.Dtos.Attendance.SessionDto>();
        foreach (var cls in classes)
        {
            var sessions = await _sessionService.GetByClassIdAsync(cls.ClassId);
            allSessions.AddRange(sessions);
        }

        if (allSessions.Count == 0)
            return [];

        var ordered = allSessions.OrderBy(s => s.Date).ToList();
        var earliest = ordered.First().Date;

        return ordered
            .GroupBy(s => GetWeekLabel(s.Date, earliest))
            .Select(g => new WeeklyAttendanceItemDto
            {
                Week = g.Key,
                Present = g.Sum(s => s.PresentCount),
                Absent = g.Sum(s => s.TotalStudents - s.PresentCount),
                Excused = 0
            })
            .OrderBy(w => w.Week)
            .ToList();
    }

    private static string GetWeekLabel(DateTime date, DateTime earliest)
    {
        var diff = (date.Date - earliest.Date).Days;
        var weekNumber = (diff / 7) + 1;
        return $"W{weekNumber}";
    }
}
