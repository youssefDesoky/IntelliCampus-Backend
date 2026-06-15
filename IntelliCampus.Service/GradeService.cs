using IntelliCampus.Domain.Entities;
using IntelliCampus.Domain.Entities.Enums;
using IntelliCampus.Domain.Interfaces;
using IntelliCampus.Service.Specifications;
using IntelliCampus.Service_Abstraction;
using IntelliCampus.Shared.Dtos.Export;
using IntelliCampus.Shared.Dtos.Grade;

namespace IntelliCampus.Service;

public class GradeService : IGradeService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly INotificationService _notificationService;
    private readonly IStudentService _studentService;
    private readonly IPdfExportService _pdfExportService;

    public GradeService(IUnitOfWork unitOfWork, INotificationService notificationService,
        IStudentService studentService, IPdfExportService pdfExportService)
    {
        _unitOfWork = unitOfWork;
        _notificationService = notificationService;
        _studentService = studentService;
        _pdfExportService = pdfExportService;
    }

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

    private IGenericRepository<Grade, int> Grades
        => _unitOfWork.GetRepository<Grade, int>();

    private IGenericRepository<Student, int> Students
        => _unitOfWork.GetRepository<Student, int>();

    private IGenericRepository<StudentCourse, (int StudentId, int CourseId)> StudentCourses
        => _unitOfWork.GetRepository<StudentCourse, (int StudentId, int CourseId)>();

    private IGenericRepository<Course, int> Courses
        => _unitOfWork.GetRepository<Course, int>();

    // Student

    public async Task<int> GetCourseWorkAsync(int studentId, int courseId)
    {
        var (assignTotalScore, assignTotalMax) = await GetAssignmentScoresAsync(studentId, courseId);
        var (quizTotalScore, quizTotalMax) = await GetQuizScoresAsync(studentId, courseId);

        var courseGrades = await Grades.GetAllAsync(new GradeSpec(studentId, courseId));
        var midterm = courseGrades.FirstOrDefault(g => g.GradeType == GradeType.Midterm && g.Status == "Graded");

        var (assignQuizContrib, midtermContrib, _) = CalculateWeightedContributions(
            assignTotalScore, assignTotalMax, quizTotalScore, quizTotalMax, midterm, null);

        return (int)Math.Round(assignQuizContrib + midtermContrib, 0);
    }

    public async Task<CourseGradeDto?> GetCourseGradeAsync(int studentId, int courseId)
    {
        var assignments = await Assignments.GetAllAsync(new AssignmentSpec(courseId, byCourse: true));
        var assignmentIds = assignments.Select(a => a.AssignmentId).ToHashSet();

        var quizzes = await Quizzes.GetAllAsync(new QuizSpec(courseId, byCourse: true));
        var quizIds = quizzes.Select(q => q.QuizId).ToHashSet();

        var mySubmissions = (await StudentAssignments.GetAllAsync(new StudentAssignmentSpec(studentId, byStudent: true, dummy: true)))
            .Where(sa => assignmentIds.Contains(sa.AssignmentId))
            .ToList();

        var myQuizSubmissions = (await StudentQuizzes.GetAllAsync(new StudentQuizSpec(studentId, true, true)))
            .Where(sq => quizIds.Contains(sq.QuizId))
            .ToList();

        var gradedAssignments = mySubmissions.Where(sa => sa.Grade.HasValue).ToList();
        var gradedQuizzes = myQuizSubmissions.Where(sq => sq.Score.HasValue).ToList();

        var courseGrades = await Grades.GetAllAsync(new GradeSpec(studentId, courseId));
        var midterm = courseGrades.FirstOrDefault(g => g.GradeType == GradeType.Midterm && g.Status == "Graded");
        var final = courseGrades.FirstOrDefault(g => g.GradeType == GradeType.Final && g.Status == "Graded");

        if (gradedAssignments.Count == 0 && gradedQuizzes.Count == 0 && midterm is null && final is null)
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
                Weight = max,
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
                Weight = max,
                Status = "Graded",
                Date = sq.SubmittedAt.ToString("dd MMM yyyy"),
                Percent = max > 0 ? Math.Round(score / max * 100, 0) : 0
            };
        }));

        if (midterm is not null)
        {
            history.Add(new GradeHistoryItemDto
            {
                Id = midterm.GradeId,
                Title = midterm.Title,
                Type = MapGradeType(GradeType.Midterm),
                Score = midterm.Score,
                MaxScore = midterm.MaxScore,
                Weight = midterm.Weight,
                Status = midterm.Status,
                Date = midterm.GradedAt.ToString("dd MMM yyyy"),
                Percent = midterm.MaxScore > 0 ? Math.Round(midterm.Score / midterm.MaxScore * 100, 0) : 0
            });
        }

        if (final is not null)
        {
            history.Add(new GradeHistoryItemDto
            {
                Id = final.GradeId,
                Title = final.Title,
                Type = MapGradeType(GradeType.Final),
                Score = final.Score,
                MaxScore = final.MaxScore,
                Weight = final.Weight,
                Status = final.Status,
                Date = final.GradedAt.ToString("dd MMM yyyy"),
                Percent = final.MaxScore > 0 ? Math.Round(final.Score / final.MaxScore * 100, 0) : 0
            });
        }

        history = history.OrderByDescending(h => h.Date).ToList();

        var assignTotalScore = gradedAssignments.Sum(sa => sa.Grade!.Value);
        var assignTotalMax = gradedAssignments.Sum(sa => assignments.First(a => a.AssignmentId == sa.AssignmentId).MaxGrade);

        var quizTotalScore = gradedQuizzes.Sum(sq => sq.Score!.Value);
        var quizTotalMax = gradedQuizzes.Sum(sq => quizzes.First(q => q.QuizId == sq.QuizId).MaxGrade);

        var breakdown = new List<AssessmentBreakdownDto>();
        if (gradedAssignments.Count > 0)
        {
            var ap = assignTotalMax > 0 ? Math.Round(assignTotalScore / assignTotalMax * 100, 0) : 0;
            breakdown.Add(new AssessmentBreakdownDto
            {
                Category = "Assignments",
                TotalScore = assignTotalScore,
                TotalMaxScore = assignTotalMax,
                TotalWeight = assignTotalMax,
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
                TotalWeight = quizTotalMax,
                Percent = qp,
                Status = "Graded"
            });
        }
        if (midterm is not null)
        {
            var mp = midterm.MaxScore > 0 ? Math.Round(midterm.Score / midterm.MaxScore * 100, 0) : 0;
            breakdown.Add(new AssessmentBreakdownDto
            {
                Category = "Midterm",
                TotalScore = midterm.Score,
                TotalMaxScore = midterm.MaxScore,
                TotalWeight = midterm.Weight,
                Percent = mp,
                Status = midterm.Status
            });
        }
        if (final is not null)
        {
            var fp = final.MaxScore > 0 ? Math.Round(final.Score / final.MaxScore * 100, 0) : 0;
            breakdown.Add(new AssessmentBreakdownDto
            {
                Category = "Final",
                TotalScore = final.Score,
                TotalMaxScore = final.MaxScore,
                TotalWeight = final.Weight,
                Percent = fp,
                Status = final.Status
            });
        }

        var (assignQuizContrib, midtermContrib, finalContrib) = CalculateWeightedContributions(
            assignTotalScore, assignTotalMax, quizTotalScore, quizTotalMax, midterm, final);

        var overallPercent = Math.Round(assignQuizContrib + midtermContrib + finalContrib, 0);

        var (letter, gpa) = await ResolveGradeScaleAsync(studentId, overallPercent);

        return new CourseGradeDto
        {
            OverallGrade = new OverallGradeDto
            {
                Percent = overallPercent,
                Letter = letter,
                Gpa = gpa
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
                    Weight = max,
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
                    Weight = max,
                    Status = "Graded",
                    Date = sq.SubmittedAt.ToString("dd MMM yyyy"),
                    Percent = max > 0 ? Math.Round(score / max * 100, 0) : 0
                };
            }));
        }

        return result.OrderByDescending(h => h.Date).ToList();
    }

    public async Task<IEnumerable<TranscriptCourseDto>> GetTranscriptAsync(int studentId)
    {
        var studentCourses = await StudentCourses.GetAllAsync(new StudentCourseIdsSpec(studentId));
        var courseIds = studentCourses.Select(sc => sc.CourseId).ToList();

        if (courseIds.Count == 0)
            return Enumerable.Empty<TranscriptCourseDto>();

        var courses = await Courses.GetAllAsync(new CourseSpec(courseIds));
        var courseDict = courses.ToDictionary(c => c.CourseId);

        var result = new List<TranscriptCourseDto>();

        foreach (var sc in studentCourses)
        {
            if (!courseDict.TryGetValue(sc.CourseId, out var course))
                continue;

            var courseId = course.CourseId;

            var (assignTotalScore, assignTotalMax) = await GetAssignmentScoresAsync(studentId, courseId);
            var (quizTotalScore, quizTotalMax) = await GetQuizScoresAsync(studentId, courseId);

            var courseGrades = await Grades.GetAllAsync(new GradeSpec(studentId, courseId));
            var midterm = courseGrades.FirstOrDefault(g => g.GradeType == GradeType.Midterm && g.Status == "Graded");
            var final = courseGrades.FirstOrDefault(g => g.GradeType == GradeType.Final && g.Status == "Graded");

            var hasCoursework = assignTotalMax > 0 || quizTotalMax > 0 || midterm is not null;

            string courseworkStr = "-";
            string totalGradeStr = "-";
            string letter = "-";

            if (hasCoursework)
            {
                var (assignQuizContrib, midtermContrib, finalContrib) = CalculateWeightedContributions(
                    assignTotalScore, assignTotalMax, quizTotalScore, quizTotalMax, midterm, final);

                courseworkStr = Math.Round(assignQuizContrib + midtermContrib, 0).ToString();

                if (final is not null)
                {
                    var overall = Math.Round(assignQuizContrib + midtermContrib + finalContrib, 0);
                    totalGradeStr = overall.ToString();
                    var (l, _) = await ResolveGradeScaleAsync(studentId, overall);
                    letter = l;
                }
                else
                {
                    totalGradeStr = "-";
                }
            }

            result.Add(new TranscriptCourseDto
            {
                CourseId = course.CourseId,
                CourseName = course.CourseName,
                CourseCode = course.CourseCode,
                CreditHours = course.CreditHours,
                Coursework = courseworkStr,
                TotalGrade = totalGradeStr,
                Letter = letter
            });
        }

        return result;
    }

    public async Task<double> GetCumulativeGpaAsync(int studentId)
    {
        var courseDtos = await GetTranscriptAsync(studentId);

        var courseItemList = courseDtos.Select(c => new TranscriptCourseItem
        {
            CourseCode = c.CourseCode,
            CourseName = c.CourseName,
            CreditHours = c.CreditHours,
            Coursework = c.Coursework,
            TotalGrade = c.TotalGrade,
            Letter = c.Letter
        }).ToList();

        var spec = new StudentSpec(studentId);
        var studentEntity = await Students.GetByIdAsync(spec);
        return CalculateGpa(courseItemList, studentEntity?.Bylaw?.GradeScales);
    }

    public async Task<double?> UpdateStudentGpaIfCompleteAsync(int studentId)
    {
        var studentCourses = (await StudentCourses.GetAllAsync(new StudentCourseIdsSpec(studentId))).ToList();
        var student = await Students.GetByIdAsync(new StudentSpec(studentId));
        if (studentCourses.Count == 0 || student is null) return student?.Gpa;

        foreach (var sc in studentCourses)
        {
            var courseGrade = await GetCourseGradeAsync(studentId, sc.CourseId);
            if (courseGrade?.OverallGrade is null) return student.Gpa;
            if (courseGrade.OverallGrade.Letter is "-" or null) return student.Gpa;
        }

        var gpa = await GetCumulativeGpaAsync(studentId);
        student.Gpa = gpa;
        await _unitOfWork.SaveChangesAsync();

        return gpa;
    }

    public async Task<byte[]> ExportTranscriptPdfAsync(int studentId)
    {
        var student = await _studentService.GetByIdAsync(studentId);
        var courseDtos = await GetTranscriptAsync(studentId);

        var studentCourses = await StudentCourses.GetAllAsync(new StudentCourseIdsSpec(studentId));
        var courseIdToSemester = studentCourses
            .Where(sc => !string.IsNullOrEmpty(sc.Semester))
            .ToDictionary(sc => sc.CourseId, sc => sc.Semester!);

        var courseItemList = courseDtos.Select(c => (
            Item: new TranscriptCourseItem
            {
                CourseCode = c.CourseCode,
                CourseName = c.CourseName,
                CreditHours = c.CreditHours,
                Coursework = c.Coursework,
                TotalGrade = c.TotalGrade,
                Letter = c.Letter
            },
            c.CourseId
        )).ToList();

        var spec = new StudentSpec(studentId);
        var studentEntity = await Students.GetByIdAsync(spec);

        int totalCredits = courseItemList.Sum(c => c.Item.CreditHours);
        double gpa = CalculateGpa(courseItemList.Select(c => c.Item).ToList(), studentEntity?.Bylaw?.GradeScales);

        var semesterGroups = courseItemList
            .GroupBy(c => courseIdToSemester.GetValueOrDefault(c.CourseId, "Courses"))
            .ToList();

        var semesters = semesterGroups
            .Select(g => new TranscriptSemesterDto
            {
                SemesterName = g.Key,
                Courses = g.Select(c => c.Item).ToList()
            })
            .OrderBy(s => s.SemesterName)
            .ToList();

        var dto = new TranscriptExportDto
        {
            StudentName = student?.FullName ?? "",
            StudentCode = student?.StudentCode ?? "-",
            Faculty = student?.FacultyName,
            Level = student?.Level,
            Department = student?.DepartmentName,
            TotalCredits = totalCredits,
            GPA = gpa,
            Semesters = semesters
        };

        return _pdfExportService.ExportTranscript(dto);
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
                Weight = assignment.MaxGrade,
                GradeType = GradeType.Assignment,
                Status = "Graded",
                GradedAt = (sa.GradedAt ?? DateTime.UtcNow).ToString("dd MM yyyy HH:mm"),
                Notes = sa.Feedback
            };
        }));

        // Quiz grades
        var quizzes = await Quizzes.GetAllAsync(new QuizSpec(courseId, byCourse: true));
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
                Weight = quiz.MaxGrade,
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

        var teachesCourse = await Classes.AnyAsync(c => c.CourseId == assignment.CourseId && c.InstructorId == instructorId);
        if (!teachesCourse)
            throw new InvalidOperationException("Not authorized.");

        complaint.Status = "Reviewed";
        Complaints.Update(complaint);
        await _unitOfWork.SaveChangesAsync();

        await _notificationService.SendAsync(
            complaint.StudentId,
            NotificationType.GradeComplaintReviewed,
            $"Your grade complaint for '{assignment.Title}' has been reviewed by your instructor.");

        return MapComplaintToDto(complaint, assignment.Title);
    }

    // Helpers

    private async Task<(decimal TotalScore, decimal TotalMax)> GetAssignmentScoresAsync(int studentId, int courseId)
    {
        var assignments = await Assignments.GetAllAsync(new AssignmentSpec(courseId, byCourse: true));
        var assignmentIds = assignments.Select(a => a.AssignmentId).ToHashSet();

        var submissions = (await StudentAssignments.GetAllAsync(new StudentAssignmentSpec(studentId, byStudent: true, dummy: true)))
            .Where(sa => assignmentIds.Contains(sa.AssignmentId) && sa.Grade.HasValue)
            .ToList();

        var totalScore = submissions.Sum(sa => sa.Grade!.Value);
        var totalMax = submissions.Sum(sa => assignments.First(a => a.AssignmentId == sa.AssignmentId).MaxGrade);

        return (totalScore, totalMax);
    }

    private async Task<(decimal TotalScore, decimal TotalMax)> GetQuizScoresAsync(int studentId, int courseId)
    {
        var quizzes = await Quizzes.GetAllAsync(new QuizSpec(courseId, byCourse: true));
        var quizIds = quizzes.Select(q => q.QuizId).ToHashSet();

        var submissions = (await StudentQuizzes.GetAllAsync(new StudentQuizSpec(studentId, true, true)))
            .Where(sq => quizIds.Contains(sq.QuizId) && sq.Score.HasValue)
            .ToList();

        var totalScore = submissions.Sum(sq => sq.Score!.Value);
        var totalMax = submissions.Sum(sq => quizzes.First(q => q.QuizId == sq.QuizId).MaxGrade);

        return (totalScore, totalMax);
    }

    private static (decimal AssignQuizContrib, decimal MidtermContrib, decimal FinalContrib) CalculateWeightedContributions(
        decimal assignTotalScore, decimal assignTotalMax,
        decimal quizTotalScore, decimal quizTotalMax,
        Grade? midterm, Grade? final)
    {
        var assignQuizWeight = 100m - (midterm?.Weight ?? 0) - (final?.Weight ?? 0);
        if (assignQuizWeight < 0) assignQuizWeight = 0;

        var assignQuizTotalScore = assignTotalScore + quizTotalScore;
        var assignQuizTotalMax = assignTotalMax + quizTotalMax;
        var assignQuizPct = assignQuizTotalMax > 0 ? assignQuizTotalScore / assignQuizTotalMax * 100 : 0;
        var assignQuizContrib = assignQuizPct * assignQuizWeight / 100;

        var midtermPct = midterm is not null && midterm.MaxScore > 0 ? midterm.Score / midterm.MaxScore * 100 : 0;
        var midtermContrib = midtermPct * (midterm?.Weight ?? 0) / 100;

        var finalPct = final is not null && final.MaxScore > 0 ? final.Score / final.MaxScore * 100 : 0;
        var finalContrib = finalPct * (final?.Weight ?? 0) / 100;

        return (assignQuizContrib, midtermContrib, finalContrib);
    }

    private async Task<(string Letter, decimal Gpa)> ResolveGradeScaleAsync(int studentId, decimal percent)
    {
        var spec = new StudentSpec(studentId);
        var student = await Students.GetByIdAsync(spec);

        var scales = student?.Bylaw?.GradeScales;
        if (scales?.Count > 0)
        {
            var scale = scales
                .OrderByDescending(s => s.MinPercentage)
                .FirstOrDefault(s => percent >= s.MinPercentage);

            if (scale is not null)
                return (scale.GradeLetter, scale.GpaValue);
        }

        return ("-", 0);
    }

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

    private static double CalculateGpa(List<TranscriptCourseItem> courses, List<GradeScaleItem>? scales)
    {
        if (courses.Count == 0) return 0.0;
        double total = 0;
        int credits = 0;
        foreach (var c in courses)
        {
            if (c.Letter is "-" or "Con" or "W" or "I") continue;
            double gp = scales?.FirstOrDefault(s => s.GradeLetter == c.Letter)?.GpaValue is decimal v
                ? (double)v
                : 0.0;
            total += gp * c.CreditHours;
            credits += c.CreditHours;
        }
        return credits > 0 ? Math.Round(total / credits, 2) : 0.0;
    }
}
