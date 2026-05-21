using IntelliCampus.Domain.Entities;
using IntelliCampus.Domain.Entities.Enums;
using IntelliCampus.Domain.Interfaces;
using IntelliCampus.Service.Specifications;
using IntelliCampus.Service_Abstraction;
using IntelliCampus.Shared.Dtos.Grade;

namespace IntelliCampus.Service;

public class GradeService : IGradeService
{
    private readonly IUnitOfWork _unitOfWork;

    public GradeService(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    private IGenericRepository<GradeComplaint, int> Complaints
        => _unitOfWork.GetRepository<GradeComplaint, int>();

    private IGenericRepository<Class, int> Classes
        => _unitOfWork.GetRepository<Class, int>();

    private IGenericRepository<StudentAssignment, int> StudentAssignments
        => _unitOfWork.GetRepository<StudentAssignment, int>();

    private IGenericRepository<Assignment, int> Assignments
        => _unitOfWork.GetRepository<Assignment, int>();

    private IGenericRepository<Quiz, int> Quizzes
        => _unitOfWork.GetRepository<Quiz, int>();

    private IGenericRepository<StudentQuiz, (int StudentId, int QuizId)> StudentQuizzes
        => _unitOfWork.GetRepository<StudentQuiz, (int StudentId, int QuizId)>();

    // Student

    public async Task<CourseGradeDto?> GetCourseGradeAsync(int studentId, int courseId)
    {
        var assignments = await Assignments.GetAllAsync(new AssignmentSpec(courseId, byCourse: true));
        var assignmentIds = assignments.Select(a => a.AssignmentId).ToHashSet();

        var quizzes = await Quizzes.GetAllAsync(new QuizSpec(courseId, "course"));
        var quizIds = quizzes.Select(q => q.QuizId).ToHashSet();

        // Assignment submissions
        var mySubmissions = (await StudentAssignments.GetAllAsync(new StudentAssignmentSpec(studentId, byStudent: true, dummy: true)))
            .Where(sa => assignmentIds.Contains(sa.AssignmentId))
            .ToList();

        // Quiz submissions
        var myQuizSubmissions = (await StudentQuizzes.GetAllAsync(new StudentQuizSpec(studentId, true, true)))
            .Where(sq => quizIds.Contains(sq.QuizId))
            .ToList();

        var gradedAssignments = mySubmissions.Where(sa => sa.Grade.HasValue).ToList();
        var gradedQuizzes = myQuizSubmissions.Where(sq => sq.Score.HasValue).ToList();

        if (gradedAssignments.Count == 0 && gradedQuizzes.Count == 0)
            return null;

        var history = new List<GradeHistoryItemDto>();

        history.AddRange(gradedAssignments.Select(sa =>
        {
            var assignment = assignments.First(a => a.AssignmentId == sa.AssignmentId);
            var max = assignment.MaxGrade;
            var score = sa.Grade!.Value;

            return new GradeHistoryItemDto
            {
                Id = sa.StudentAssignmentId,
                Title = assignment.Title,
                Type = MapGradeType(GradeType.Assignment),
                Score = score,
                MaxScore = max,
                Weight = 1,
                Status = "Graded",
                Date = (sa.GradedAt ?? sa.SubmittedAt).ToString("dd MMM yyyy"),
                Percent = max > 0 ? Math.Round(score / max * 100, 0) : 0
            };
        }));

        history.AddRange(gradedQuizzes.Select(sq =>
        {
            var quiz = quizzes.First(q => q.QuizId == sq.QuizId);
            var max = quiz.MaxGrade;
            var score = sq.Score!.Value;

            return new GradeHistoryItemDto
            {
                Id = sq.QuizId,
                Title = quiz.Title,
                Type = MapGradeType(GradeType.Quiz),
                Score = score,
                MaxScore = max,
                Weight = 1,
                Status = "Graded",
                Date = sq.SubmittedAt.ToString("dd MMM yyyy"),
                Percent = max > 0 ? Math.Round(score / max * 100, 0) : 0
            };
        }));

        history = history.OrderByDescending(h => h.Date).ToList();

        var assignTotalScore = gradedAssignments.Sum(sa => sa.Grade!.Value);
        var assignTotalMax = gradedAssignments.Sum(sa => assignments.First(a => a.AssignmentId == sa.AssignmentId).MaxGrade);

        var quizTotalScore = gradedQuizzes.Sum(sq => sq.Score!.Value);
        var quizTotalMax = gradedQuizzes.Sum(sq => quizzes.First(q => q.QuizId == sq.QuizId).MaxGrade);

        var allScore = assignTotalScore + quizTotalScore;
        var allMax = assignTotalMax + quizTotalMax;
        var overallPercent = allMax > 0 ? Math.Round(allScore / allMax * 100, 0) : 0;

        var breakdown = new List<AssessmentBreakdownDto>();
        if (gradedAssignments.Count > 0)
        {
            var ap = assignTotalMax > 0 ? Math.Round(assignTotalScore / assignTotalMax * 100, 0) : 0;
            breakdown.Add(new AssessmentBreakdownDto
            {
                Category = "Assignments",
                TotalScore = assignTotalScore,
                TotalMaxScore = assignTotalMax,
                TotalWeight = 1,
                Percent = ap,
                Status = "Graded"
            });
        }
        if (gradedQuizzes.Count > 0)
        {
            var qp = quizTotalMax > 0 ? Math.Round(quizTotalScore / quizTotalMax * 100, 0) : 0;
            breakdown.Add(new AssessmentBreakdownDto
            {
                Category = "Quizzes",
                TotalScore = quizTotalScore,
                TotalMaxScore = quizTotalMax,
                TotalWeight = 1,
                Percent = qp,
                Status = "Graded"
            });
        }

        return new CourseGradeDto
        {
            OverallGrade = new OverallGradeDto
            {
                Percent = overallPercent,
                Letter = GetLetterGrade(overallPercent)
            },
            AssessmentBreakdown = breakdown,
            History = history
        };
    }

    public async Task<IEnumerable<GradeHistoryItemDto>> GetAllGradesAsync(int studentId)
    {
        var result = new List<GradeHistoryItemDto>();

        // Assignment grades
        var mySubmissions = await StudentAssignments.GetAllAsync(new StudentAssignmentSpec(studentId, byStudent: true, dummy: true));
        var gradedAssignments = mySubmissions.Where(sa => sa.Grade.HasValue).ToList();

        if (gradedAssignments.Count > 0)
        {
            var assignmentIds = gradedAssignments.Select(sa => sa.AssignmentId).Distinct().ToList();
            var assignments = (await Assignments.GetAllAsync()).Where(a => assignmentIds.Contains(a.AssignmentId)).ToList();

            result.AddRange(gradedAssignments.Select(sa =>
            {
                var assignment = assignments.First(a => a.AssignmentId == sa.AssignmentId);
                var max = assignment.MaxGrade;
                var score = sa.Grade!.Value;
                return new GradeHistoryItemDto
                {
                    Id = sa.StudentAssignmentId,
                    Title = assignment.Title,
                    Type = MapGradeType(GradeType.Assignment),
                    Score = score,
                    MaxScore = max,
                    Weight = 1,
                    Status = "Graded",
                    Date = (sa.GradedAt ?? sa.SubmittedAt).ToString("dd MMM yyyy"),
                    Percent = max > 0 ? Math.Round(score / max * 100, 0) : 0
                };
            }));
        }

        // Quiz grades
        var myQuizSubmissions = await StudentQuizzes.GetAllAsync(new StudentQuizSpec(studentId, true, true));
        var gradedQuizzes = myQuizSubmissions.Where(sq => sq.Score.HasValue).ToList();

        if (gradedQuizzes.Count > 0)
        {
            var quizIds = gradedQuizzes.Select(sq => sq.QuizId).Distinct().ToList();
            var quizzes = (await Quizzes.GetAllAsync()).Where(q => quizIds.Contains(q.QuizId)).ToList();

            result.AddRange(gradedQuizzes.Select(sq =>
            {
                var quiz = quizzes.First(q => q.QuizId == sq.QuizId);
                var max = quiz.MaxGrade;
                var score = sq.Score!.Value;
                return new GradeHistoryItemDto
                {
                    Id = sq.QuizId,
                    Title = quiz.Title,
                    Type = MapGradeType(GradeType.Quiz),
                    Score = score,
                    MaxScore = max,
                    Weight = 1,
                    Status = "Graded",
                    Date = sq.SubmittedAt.ToString("dd MMM yyyy"),
                    Percent = max > 0 ? Math.Round(score / max * 100, 0) : 0
                };
            }));
        }

        return result.OrderByDescending(h => h.Date).ToList();
    }

    // Instructor (read-only)

    public async Task<IEnumerable<GradeDto>> GetByStudentAndCourseAsync(int instructorId, int studentId, int courseId)
    {
        var teaches = await Classes.AnyAsync(c => c.CourseId == courseId && c.InstructorId == instructorId);
        if (!teaches)
            throw new InvalidOperationException("Not authorized.");

        var result = new List<GradeDto>();

        // Assignment grades
        var assignments = await Assignments.GetAllAsync(new AssignmentSpec(courseId, byCourse: true));
        var assignmentIds = assignments.Select(a => a.AssignmentId).ToHashSet();

        var submissions = (await StudentAssignments.GetAllAsync(new StudentAssignmentSpec(studentId, byStudent: true, dummy: true)))
            .Where(sa => assignmentIds.Contains(sa.AssignmentId) && sa.Grade.HasValue)
            .ToList();

        result.AddRange(submissions.Select(sa =>
        {
            var assignment = assignments.First(a => a.AssignmentId == sa.AssignmentId);
            return new GradeDto
            {
                GradeId = sa.StudentAssignmentId,
                StudentId = studentId,
                CourseId = courseId,
                CourseName = null,
                Title = assignment.Title,
                Score = sa.Grade!.Value,
                MaxScore = assignment.MaxGrade,
                Weight = 1,
                GradeType = GradeType.Assignment,
                Status = "Graded",
                GradedAt = (sa.GradedAt ?? DateTime.UtcNow).ToString("dd MM yyyy HH:mm"),
                Notes = sa.Feedback
            };
        }));

        // Quiz grades
        var quizzes = await Quizzes.GetAllAsync(new QuizSpec(courseId, "course"));
        var quizIds = quizzes.Select(q => q.QuizId).ToHashSet();

        var quizSubmissions = (await StudentQuizzes.GetAllAsync(new StudentQuizSpec(studentId, true, true)))
            .Where(sq => quizIds.Contains(sq.QuizId) && sq.Score.HasValue)
            .ToList();

        result.AddRange(quizSubmissions.Select(sq =>
        {
            var quiz = quizzes.First(q => q.QuizId == sq.QuizId);
            return new GradeDto
            {
                GradeId = sq.QuizId,
                StudentId = studentId,
                CourseId = courseId,
                CourseName = null,
                Title = quiz.Title,
                Score = sq.Score!.Value,
                MaxScore = quiz.MaxGrade,
                Weight = 1,
                GradeType = GradeType.Quiz,
                Status = "Graded",
                GradedAt = sq.SubmittedAt.ToString("dd MM yyyy HH:mm"),
                Notes = null
            };
        }));

        return result;
    }

    // Complaints

    public async Task<GradeComplaintResponseDto> FileComplaintAsync(int studentId, GradeComplaintDto dto)
    {
        // dto.GradeId is treated as StudentAssignmentId for now.
        var submission = await StudentAssignments.GetByIdAsync(dto.GradeId);
        if (submission is null || submission.StudentId != studentId)
            throw new InvalidOperationException("Grade not found.");

        if (!submission.Grade.HasValue)
            throw new InvalidOperationException("Cannot complain about an ungraded submission.");

        var alreadyFiled = await Complaints.AnyAsync(c => c.GradeId == dto.GradeId && c.StudentId == studentId && c.Status == "Pending");
        if (alreadyFiled)
            throw new InvalidOperationException("You already have a pending complaint for this grade.");

        var complaint = new GradeComplaint
        {
            GradeId = dto.GradeId,
            StudentId = studentId,
            ComplaintType = dto.ComplaintType,
            Details = dto.Details,
            Status = "Pending",
            SubmittedAt = DateTime.UtcNow
        };

        Complaints.Add(complaint);
        await _unitOfWork.SaveChangesAsync();

        var assignment = await Assignments.GetByIdAsync(submission.AssignmentId);
        return MapComplaintToDto(complaint, assignment?.Title ?? string.Empty);
    }

    public async Task<IEnumerable<GradeComplaintResponseDto>> GetComplaintsAsync(int studentId)
    {
        var spec = new GradeComplaintSpec(studentId);
        var complaints = await Complaints.GetAllAsync(spec);

        // GradeComplaintSpec includes Grade navigation which won't be populated in this mode.
        // Resolve titles from StudentAssignment -> Assignment.
        var assignmentTitles = new Dictionary<int, string>();

        var result = new List<GradeComplaintResponseDto>();
        foreach (var c in complaints)
        {
            if (!assignmentTitles.TryGetValue(c.GradeId, out var title))
            {
                var submission = await StudentAssignments.GetByIdAsync(c.GradeId);
                var assignment = submission is null ? null : await Assignments.GetByIdAsync(submission.AssignmentId);
                title = assignment?.Title ?? string.Empty;
                assignmentTitles[c.GradeId] = title;
            }

            result.Add(MapComplaintToDto(c, title));
        }

        return result;
    }

    public async Task<GradeComplaintResponseDto?> ReviewComplaintAsync(int complaintId, int instructorId)
    {
        var complaint = await Complaints.GetByIdAsync(complaintId);
        if (complaint is null) return null;

        // complaint.GradeId is StudentAssignmentId
        var submission = await StudentAssignments.GetByIdAsync(complaint.GradeId);
        if (submission is null) return null;

        var assignment = await Assignments.GetByIdAsync(submission.AssignmentId);
        if (assignment is null) return null;

        var cls = await Classes.GetByIdAsync(assignment.ClassId);
        if (cls?.InstructorId != instructorId)
            throw new InvalidOperationException("Not authorized.");

        complaint.Status = "Reviewed";
        Complaints.Update(complaint);
        await _unitOfWork.SaveChangesAsync();

        return MapComplaintToDto(complaint, assignment.Title);
    }

    // Helpers

    private static string GetLetterGrade(decimal percent) => percent switch
    {
        >= 96 => "A+",
        >= 93 => "A",
        >= 90 => "A-",
        >= 87 => "B+",
        >= 83 => "B",
        >= 80 => "B-",
        >= 77 => "C+",
        >= 73 => "C",
        >= 70 => "C-",
        >= 67 => "D+",
        >= 60 => "D",
        _ => "F"
    };

    private static string MapGradeType(GradeType type) => type switch
    {
        GradeType.Quiz => "quiz",
        GradeType.Assignment => "assignment",
        GradeType.Midterm => "midterm",
        GradeType.Final => "final",
        _ => "other"
    };

    private static GradeComplaintResponseDto MapComplaintToDto(GradeComplaint c, string gradeTitle) => new()
    {
        ComplaintId = c.ComplaintId,
        GradeId = c.GradeId,
        Title = gradeTitle,
        ComplaintType = c.ComplaintType,
        Details = c.Details,
        Status = c.Status,
        SubmittedAt = c.SubmittedAt.ToString("dd MM yyyy HH:mm")
    };
}
