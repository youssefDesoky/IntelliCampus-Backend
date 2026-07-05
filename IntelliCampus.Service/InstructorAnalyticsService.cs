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
            WeeklyAttendance = await BuildWeeklyAttendanceAsync(courseId),
            CourseWorkBreakdown = await BuildCourseWorkBreakdownAsync(courseId),
            StudentScoreHeatmap = await BuildStudentScoreHeatmapAsync(courseId, students)
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

    private IGenericRepository<CourseWorkWeight, int> CourseWorkWeightRepo
        => _unitOfWork.GetRepository<CourseWorkWeight, int>();

    private async Task<CourseWorkBreakdownDto> BuildCourseWorkBreakdownAsync(int courseId)
    {
        var weight = await CourseWorkWeightRepo.GetByIdAsync(courseId);
        if (weight is not null && (weight.QuizWeight > 0 || weight.AssignmentWeight > 0 || weight.MidtermWeight > 0))
        {
            var items = new List<CourseWorkBreakdownItemDto>();

            if (weight.QuizWeight > 0)
                items.Add(new CourseWorkBreakdownItemDto { Type = "Quiz", Marks = weight.QuizWeight });
            if (weight.AssignmentWeight > 0)
                items.Add(new CourseWorkBreakdownItemDto { Type = "Assignment", Marks = weight.AssignmentWeight });
            if (weight.MidtermWeight > 0)
                items.Add(new CourseWorkBreakdownItemDto { Type = "Midterm", Marks = weight.MidtermWeight });

            var declared = weight.QuizWeight + weight.AssignmentWeight + weight.MidtermWeight;
            var undeclared = declared < 100 ? 100 - declared : 0;

            return new CourseWorkBreakdownDto
            {
                TotalMarks = 100,
                Breakdown = items,
                UndeclaredMarks = undeclared
            };
        }

        // Fallback: build from actual assessment marks when weights are not configured
        var fallbackItems = new List<CourseWorkBreakdownItemDto>();

        var quizzes = await _quizService.GetByCourseIdAsync(courseId);
        var quizMarks = quizzes.Sum(q => (double)q.MaxScore);
        if (quizMarks > 0)
            fallbackItems.Add(new CourseWorkBreakdownItemDto { Type = "Quiz", Marks = (decimal)quizMarks });

        var assignments = await _assignmentService.GetByCourseIdAsync(courseId);
        var assignmentMarks = assignments.Sum(a => (double)a.TotalPoints);
        if (assignmentMarks > 0)
            fallbackItems.Add(new CourseWorkBreakdownItemDto { Type = "Assignment", Marks = (decimal)assignmentMarks });

        var examRepo = _unitOfWork.GetRepository<Exam, int>();
        var exams = await examRepo.GetAllAsync(new ExamByCourseIdSpec(courseId), asNoTracking: true);
        var midtermMarks = exams.Where(e => e.ExamType == ExamType.Midterm).Sum(e => (double)e.MaxGrade);
        var finalMarks = exams.Where(e => e.ExamType == ExamType.Final).Sum(e => (double)e.MaxGrade);
        if (midtermMarks > 0)
            fallbackItems.Add(new CourseWorkBreakdownItemDto { Type = "Midterm", Marks = (decimal)midtermMarks });
        if (finalMarks > 0)
            fallbackItems.Add(new CourseWorkBreakdownItemDto { Type = "Final", Marks = (decimal)finalMarks });

        var total = fallbackItems.Sum(i => (double)i.Marks);
        if (total <= 0)
            return new CourseWorkBreakdownDto();

        return new CourseWorkBreakdownDto
        {
            TotalMarks = (decimal)total,
            Breakdown = fallbackItems,
            UndeclaredMarks = 0
        };
    }

    private async Task<List<StudentScoreHeatmapRowDto>> BuildStudentScoreHeatmapAsync(int courseId, IEnumerable<IntelliCampus.Shared.Dtos.Student.StudentDto> students)
    {
        var studentMap = students.ToDictionary(s => s.StudentId, s => s.FullName);
        var studentScores = new Dictionary<int, Dictionary<string, double>>();

        var quizzes = await _quizService.GetByCourseIdAsync(courseId);
        var quizIds = quizzes.Select(q => q.Id).ToHashSet();
        if (quizIds.Count > 0)
        {
            var studentQuizRepo = _unitOfWork.GetRepository<StudentQuiz, int>();
            var allQuizResults = await studentQuizRepo.GetAllAsync(new StudentQuizSpec(quizIds, true), asNoTracking: true);
            var quizMaxMap = quizzes.ToDictionary(q => q.Id, q => (double)q.MaxScore);
            var quizTitleMap = quizzes.ToDictionary(q => q.Id, q => q.Title);

            foreach (var sq in allQuizResults)
            {
                if (!sq.Score.HasValue || !quizMaxMap.TryGetValue(sq.QuizId, out var max) || max <= 0)
                    continue;

                if (!quizTitleMap.TryGetValue(sq.QuizId, out var title))
                    continue;

                if (!studentScores.TryGetValue(sq.StudentId, out var scores))
                {
                    scores = new Dictionary<string, double>();
                    studentScores[sq.StudentId] = scores;
                }
                scores[title] = Math.Round((double)sq.Score.Value / max * 100, 1);
            }
        }

        var assignments = await _assignmentService.GetByCourseIdAsync(courseId);
        var assignmentIds = assignments.Select(a => int.Parse(a.Id)).ToHashSet();
        if (assignmentIds.Count > 0)
        {
            var studentAssignmentRepo = _unitOfWork.GetRepository<StudentAssignment, int>();
            var allSubmissions = await studentAssignmentRepo.GetAllAsync(new StudentAssignmentSpec(assignmentIds, true), asNoTracking: true);
            var assignMaxMap = assignments.ToDictionary(a => int.Parse(a.Id), a => (double)a.TotalPoints);
            var assignTitleMap = assignments.ToDictionary(a => int.Parse(a.Id), a => a.Title);

            foreach (var sa in allSubmissions)
            {
                if (!sa.Grade.HasValue || !assignMaxMap.TryGetValue(sa.AssignmentId, out var max) || max <= 0)
                    continue;

                if (!assignTitleMap.TryGetValue(sa.AssignmentId, out var title))
                    continue;

                if (!studentScores.TryGetValue(sa.StudentId, out var scores))
                {
                    scores = new Dictionary<string, double>();
                    studentScores[sa.StudentId] = scores;
                }
                scores[title] = Math.Round((double)sa.Grade.Value / max * 100, 1);
            }
        }

        return studentScores
            .Select(kvp => new StudentScoreHeatmapRowDto
            {
                Student = studentMap.TryGetValue(kvp.Key, out var name) ? name : $"Student #{kvp.Key}",
                Scores = kvp.Value
            })
            .Where(r => r.Scores.Count > 0)
            .ToList();
    }
}
