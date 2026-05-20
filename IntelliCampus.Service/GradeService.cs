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

    // Student

    public async Task<CourseGradeDto?> GetCourseGradeAsync(int studentId, int courseId)
    {
        var assignments = await Assignments.GetAllAsync(new AssignmentSpec(courseId, byCourse: true));
        var assignmentIds = assignments.Select(a => a.AssignmentId).ToHashSet();

        // Pull student submissions for this course
        var mySubmissions = (await StudentAssignments.GetAllAsync(new StudentAssignmentSpec(studentId, byStudent: true, dummy: true)))
            .Where(sa => assignmentIds.Contains(sa.AssignmentId))
            .ToList();

        var graded = mySubmissions.Where(sa => sa.Grade.HasValue).ToList();
        if (graded.Count == 0)
            return null;

        var history = graded.Select(sa =>
        {
            var assignment = assignments.First(a => a.AssignmentId == sa.AssignmentId);
            var max = assignment.MaxGrade;
            var score = sa.Grade!.Value;

            return new GradeHistoryItemDto
            {
                // NOTE: For now we treat StudentAssignmentId as the "grade id" for complaints.
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
        }).OrderByDescending(h => h.Date).ToList();

        var totalScore = graded.Sum(sa => sa.Grade!.Value);
        var totalMax = graded.Sum(sa => assignments.First(a => a.AssignmentId == sa.AssignmentId).MaxGrade);

        var overallPercent = totalMax > 0 ? Math.Round(totalScore / totalMax * 100, 0) : 0;

        return new CourseGradeDto
        {
            OverallGrade = new OverallGradeDto
            {
                Percent = overallPercent,
                Letter = GetLetterGrade(overallPercent)
            },
            AssessmentBreakdown =
            [
                new AssessmentBreakdownDto
                {
                    Category = "Assignments",
                    TotalScore = totalScore,
                    TotalMaxScore = totalMax,
                    TotalWeight = 1,
                    Percent = overallPercent,
                    Status = "Graded"
                }
            ],
            History = history
        };
    }

    public async Task<IEnumerable<GradeHistoryItemDto>> GetAllGradesAsync(int studentId)
    {
        var mySubmissions = await StudentAssignments.GetAllAsync(new StudentAssignmentSpec(studentId, byStudent: true, dummy: true));
        var graded = mySubmissions.Where(sa => sa.Grade.HasValue).ToList();

        if (graded.Count == 0)
            return [];

        // load needed assignments in one shot
        var assignmentIds = graded.Select(sa => sa.AssignmentId).Distinct().ToList();
        var assignments = (await Assignments.GetAllAsync()).Where(a => assignmentIds.Contains(a.AssignmentId)).ToList();

        return graded.Select(sa =>
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
        });
    }

    // Instructor (read-only)

    public async Task<IEnumerable<GradeDto>> GetByStudentAndCourseAsync(int instructorId, int studentId, int courseId)
    {
        var teaches = await Classes.AnyAsync(c => c.CourseId == courseId && c.InstructorId == instructorId);
        if (!teaches)
            throw new InvalidOperationException("Not authorized.");

        var assignments = await Assignments.GetAllAsync(new AssignmentSpec(courseId, byCourse: true));
        var assignmentIds = assignments.Select(a => a.AssignmentId).ToHashSet();

        var submissions = (await StudentAssignments.GetAllAsync(new StudentAssignmentSpec(studentId, byStudent: true, dummy: true)))
            .Where(sa => assignmentIds.Contains(sa.AssignmentId) && sa.Grade.HasValue)
            .ToList();

        return submissions.Select(sa =>
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
                GradedAt = sa.GradedAt ?? DateTime.UtcNow,
                Notes = sa.Feedback
            };
        });
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
        SubmittedAt = c.SubmittedAt
    };
}
