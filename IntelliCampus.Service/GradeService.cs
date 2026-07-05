using IntelliCampus.Domain.Entities;
using IntelliCampus.Domain.Entities.Enums;
using IntelliCampus.Domain.Helpers;
using IntelliCampus.Domain.Interfaces;
using IntelliCampus.Service.Specifications;
using IntelliCampus.Service_Abstraction;
using IntelliCampus.shared.Pagination;
using IntelliCampus.Shared.Dtos.Export;
using IntelliCampus.Shared.Dtos.Grade;
using IntelliCampus.Shared.Params;
using IntelliCampus.Service.Exceptions;

namespace IntelliCampus.Service;

public class GradeService : IGradeService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly INotificationService _notificationService;
    private readonly IPdfExportService _pdfExportService;
    private readonly IBylawService _bylawService;

    public GradeService(IUnitOfWork unitOfWork, INotificationService notificationService,
        IPdfExportService pdfExportService, IBylawService bylawService)
    {
        _unitOfWork = unitOfWork;
        _notificationService = notificationService;
        _pdfExportService = pdfExportService;
        _bylawService = bylawService;
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

    private async Task EnsureCourseActiveAsync(int courseId)
    {
        var course = await Courses.GetByIdAsync(courseId);
        if (course is null) throw new KeyNotFoundException("Course not found.");
        if (course.Status != CourseStatus.Active)
            throw new InvalidOperationException("This course is finalized and read-only.");
    }

    private async Task EnsureStudentEnrollmentActiveAsync(int studentId, int courseId)
    {
        var enrollment = await _unitOfWork.GetRepository<StudentCourse, (int, int)>().GetByIdAsync((studentId, courseId));
        if (enrollment is null || (enrollment.Status != StudentCourseStatus.InProgress && enrollment.Status != StudentCourseStatus.Registered))
            throw new InvalidOperationException("This course has ended and is read-only.");
    }

    private IGenericRepository<CourseWorkWeight, int> CourseWorkWeights
        => _unitOfWork.GetRepository<CourseWorkWeight, int>();

    // Student

    public async Task<int> GetCourseWorkAsync(int studentId, int courseId)
    {
        var student = await Students.GetByIdAsync(studentId);
        if (student is null)
            throw new StudentNotFoundException(studentId);

        var course = await Courses.GetByIdAsync(courseId);
        if (course is null)
            throw new CourseNotFoundException(courseId);

        var (assignTotalScore, assignTotalMax) = await GetAssignmentScoresAsync(studentId, courseId);
        var (quizTotalScore, quizTotalMax) = await GetQuizScoresAsync(studentId, courseId);

        var courseGrades = await Grades.GetAllAsync(new GradeSpec(studentId, courseId), asNoTracking: true);
        var midterm = courseGrades.FirstOrDefault(g => g.GradeType == GradeType.Midterm && g.Status == "Graded");

        var courseWorkWeight = await CourseWorkWeights.GetByIdAsync(courseId);
        var midtermWeight = courseWorkWeight?.MidtermWeight ?? midterm?.Weight ?? 0;
        var assignmentWeight = courseWorkWeight?.AssignmentWeight ?? 0;
        var quizWeight = courseWorkWeight?.QuizWeight ?? 0;

        if (courseWorkWeight is null)
        {
            var (aqContrib, mtContrib, _) = CalculateWeightedContributions(
                assignTotalScore, assignTotalMax, quizTotalScore, quizTotalMax, midterm, null);
            return (int)Math.Round(aqContrib + mtContrib, 0);
        }

        var (assignmentContrib, quizContrib, midContrib, _) = CalculateContributions(
            assignTotalScore, assignTotalMax, quizTotalScore, quizTotalMax,
            assignmentWeight, quizWeight, midtermWeight, 0, midterm, null);

        return (int)Math.Round(assignmentContrib + quizContrib + midContrib, 0);
    }

    public async Task<CourseGradeDto?> GetCourseGradeAsync(int studentId, int courseId)
    {
        var student = await Students.GetByIdAsync(studentId);
        if (student is null)
            throw new StudentNotFoundException(studentId);

        var course = await Courses.GetByIdAsync(courseId);
        if (course is null)
            throw new CourseNotFoundException(courseId);

        var assignments = await Assignments.GetAllAsync(new AssignmentSpec(courseId, byCourse: true), asNoTracking: true);
        var assignmentIds = assignments.Select(a => a.AssignmentId).ToHashSet();

        var quizzes = await Quizzes.GetAllAsync(new QuizSpec(courseId, byCourse: true), asNoTracking: true);
        var quizIds = quizzes.Select(q => q.QuizId).ToHashSet();

        var mySubmissions = (await StudentAssignments.GetAllAsync(new StudentAssignmentSpec(studentId, byStudent: true, dummy: true), asNoTracking: true))
            .Where(sa => assignmentIds.Contains(sa.AssignmentId))
            .ToList();

        var myQuizSubmissions = (await StudentQuizzes.GetAllAsync(new StudentQuizSpec(studentId, true, true), asNoTracking: true))
            .Where(sq => quizIds.Contains(sq.QuizId))
            .ToList();

        var gradedAssignments = mySubmissions.Where(sa => sa.Grade.HasValue).ToList();
        var gradedQuizzes = myQuizSubmissions.Where(sq => sq.Score.HasValue).ToList();

        var courseGrades = await Grades.GetAllAsync(new GradeSpec(studentId, courseId), asNoTracking: true);
        var midterm = courseGrades.FirstOrDefault(g => g.GradeType == GradeType.Midterm && g.Status == "Graded");
        var final = courseGrades.FirstOrDefault(g => g.GradeType == GradeType.Final && g.Status == "Graded");

        if (gradedAssignments.Count == 0 && gradedQuizzes.Count == 0 && midterm is null && final is null)
            return new CourseGradeDto();

        var history = new List<GradeHistoryItemDto>();

        foreach (var sa in gradedAssignments)
        {
            var assignment = assignments.FirstOrDefault(a => a.AssignmentId == sa.AssignmentId);
            if (assignment is null) continue;
            var max = assignment.MaxGrade;
            var score = sa.Grade!.Value;

            history.Add(new GradeHistoryItemDto
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
            });
        }

        foreach (var sq in gradedQuizzes)
        {
            var quiz = quizzes.FirstOrDefault(q => q.QuizId == sq.QuizId);
            if (quiz is null) continue;
            var max = quiz.MaxGrade;
            var score = sq.Score!.Value;

            history.Add(new GradeHistoryItemDto
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
            });
        }

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

        var (assignTotalScore, assignTotalMax) = ComputeAssignmentGrade(gradedAssignments, assignments);
        var (quizTotalScore, quizTotalMax) = ComputeQuizGrade(gradedQuizzes, quizzes);

        var courseWorkWeight = await CourseWorkWeights.GetByIdAsync(courseId);
        var studentBylawSettings = student.Bylaw?.Settings;

        var assignWeight = courseWorkWeight?.AssignmentWeight ?? assignTotalMax;
        var quizWeight = courseWorkWeight?.QuizWeight ?? quizTotalMax;
        var midtermDisplayWeight = courseWorkWeight?.MidtermWeight ?? midterm?.Weight ?? 0;
        var finalDisplayWeight = courseWorkWeight is not null
            ? (studentBylawSettings?.FinalExamGrade ?? final?.Weight ?? 0)
            : (final?.Weight ?? 0);

        var breakdown = new List<AssessmentBreakdownDto>();
        if (gradedAssignments.Count > 0)
        {
            var ap = assignTotalMax > 0 ? Math.Round(assignTotalScore / assignTotalMax * 100, 0) : 0;
            breakdown.Add(new AssessmentBreakdownDto
            {
                Category = "Assignments",
                TotalScore = assignTotalScore,
                TotalMaxScore = assignTotalMax,
                TotalWeight = assignWeight,
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
                TotalWeight = quizWeight,
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
                TotalWeight = midtermDisplayWeight,
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
                TotalWeight = finalDisplayWeight,
                Percent = fp,
                Status = final.Status
            });
        }

        decimal overallPercent;
        decimal midtermWeight;
        decimal finalWeight;
        decimal assignQuizWeight;
        decimal assignQuizContrib = 0;
        decimal midtermContrib = 0;
        decimal finalContrib = 0;

        if (courseWorkWeight is not null)
        {
            var (assignmentContrib, quizContrib, mtContrib, fnContrib) = CalculateContributions(
                assignTotalScore, assignTotalMax, quizTotalScore, quizTotalMax,
                courseWorkWeight.AssignmentWeight, courseWorkWeight.QuizWeight,
                courseWorkWeight.MidtermWeight, studentBylawSettings?.FinalExamGrade ?? final?.Weight ?? 0,
                midterm, final);

            assignQuizContrib = assignmentContrib + quizContrib;
            midtermContrib = mtContrib;
            finalContrib = fnContrib;
            overallPercent = Math.Round(assignQuizContrib + midtermContrib + finalContrib, 0);
            midtermWeight = courseWorkWeight.MidtermWeight;
            finalWeight = studentBylawSettings?.FinalExamGrade ?? final?.Weight ?? 0;
            assignQuizWeight = courseWorkWeight.AssignmentWeight + courseWorkWeight.QuizWeight;
        }
        else
        {
            (assignQuizContrib, midtermContrib, finalContrib) = CalculateWeightedContributions(
                assignTotalScore, assignTotalMax, quizTotalScore, quizTotalMax, midterm, final);

            overallPercent = Math.Round(assignQuizContrib + midtermContrib + finalContrib, 0);
            midtermWeight = midterm?.Weight ?? 0;
            finalWeight = final?.Weight ?? 0;
            assignQuizWeight = 100 - midtermWeight - finalWeight;
        }

        (overallPercent, var isForcedFailing) = await ApplyBylawGradeRulesAsync(studentId, courseId,
            assignQuizWeight, midtermWeight, finalWeight,
            assignQuizContrib, midtermContrib, finalContrib, overallPercent);

        var (letter, gpa) = await ResolveGradeScaleAsync(studentId, overallPercent);

        if (isForcedFailing)
        {
            var scales = student.Bylaw?.GradeScales;
            if (scales?.Count > 0)
            {
                var lowest = scales.OrderByDescending(s => s.SortOrder).First();
                letter = lowest.GradeLetter;
                gpa = lowest.GpaValue;
            }
            else
            {
                letter = "F";
                gpa = 0;
            }
        }

        return BuildCourseGradeDto(overallPercent, letter, gpa, breakdown, history);
    }

    public async Task<PaginatedResult<CourseGradeDto>> GetCourseGradeAsync(int studentId, int courseId, GradeQueryParams queryParams)
    {
        var result = await GetCourseGradeAsync(studentId, courseId);
        var wrapped = new List<CourseGradeDto> { result };
        return new PaginatedResult<CourseGradeDto>(queryParams.PageIndex, wrapped.Count, wrapped.Count, wrapped);
    }

    public async Task<IEnumerable<GradeHistoryItemDto>> GetAllGradesAsync(int studentId)
    {
        var student = await Students.GetByIdAsync(studentId);
        if (student is null)
            throw new StudentNotFoundException(studentId);

        var result = new List<GradeHistoryItemDto>();

        // Assignment grades
        var mySubmissions = await StudentAssignments.GetAllAsync(new StudentAssignmentSpec(studentId, byStudent: true, dummy: true), asNoTracking: true);
        var gradedAssignments = mySubmissions.Where(sa => sa.Grade.HasValue).ToList();

        if (gradedAssignments.Count > 0)
        {
            var assignmentIds = gradedAssignments.Select(sa => sa.AssignmentId).Distinct().ToList();
            var assignments = (await Assignments.GetAllAsync(new AssignmentSpec(assignmentIds, byIds: true), asNoTracking: true)).ToList();

            foreach (var sa in gradedAssignments)
            {
                var assignment = assignments.FirstOrDefault(a => a.AssignmentId == sa.AssignmentId);
                if (assignment is null) continue;
                var max = assignment.MaxGrade;
                var score = sa.Grade!.Value;
                result.Add(new GradeHistoryItemDto
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
                });
            }
        }

        // Quiz grades
        var myQuizSubmissions = await StudentQuizzes.GetAllAsync(new StudentQuizSpec(studentId, true, true), asNoTracking: true);
        var gradedQuizzes = myQuizSubmissions.Where(sq => sq.Score.HasValue).ToList();

        if (gradedQuizzes.Count > 0)
        {
            var quizIds = gradedQuizzes.Select(sq => sq.QuizId).Distinct().ToList();
            var quizzes = (await Quizzes.GetAllAsync(new QuizSpec(quizIds, byIds: true), asNoTracking: true)).ToList();

            foreach (var sq in gradedQuizzes)
            {
                var quiz = quizzes.FirstOrDefault(q => q.QuizId == sq.QuizId);
                if (quiz is null) continue;
                var max = quiz.MaxGrade;
                var score = sq.Score!.Value;
                result.Add(new GradeHistoryItemDto
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
                });
            }
        }

        return result.OrderByDescending(h => h.Date).ToList();
    }

    public async Task<IEnumerable<TranscriptCourseDto>> GetTranscriptAsync(int studentId)
    {
        var studentCourses = (await StudentCourses.GetAllAsync(new StudentCourseIdsSpec(studentId), asNoTracking: true)).ToList();
        var courseIds = studentCourses.Select(sc => sc.CourseId).ToList();

        if (courseIds.Count == 0)
            return Enumerable.Empty<TranscriptCourseDto>();

        var courses = await Courses.GetAllAsync(new CourseBasicSpec(courseIds), asNoTracking: true);
        var courseDict = courses.ToDictionary(c => c.CourseId);

        var student = await Students.GetByIdAsync(new StudentSpec(new CourseQueryParams { StudentId = studentId }, lightweight: true));
        var effectiveCredits = await _bylawService.GetEffectiveCreditHoursAsync(
            student?.BylawId ?? 0, student?.DepartmentId);

       
        var assignments = await Assignments.GetAllAsync(new AssignmentSpec(courseIds), asNoTracking: true);
        var quizzes = await Quizzes.GetAllAsync(new QuizSpec(courseIds), asNoTracking: true);
        var studentAssignments = (await StudentAssignments.GetAllAsync(new StudentAssignmentSpec(studentId, "transcript"), asNoTracking: true)).ToList();
        var studentQuizzes = (await StudentQuizzes.GetAllAsync(new StudentQuizSpec(studentId, "transcript"), asNoTracking: true)).ToList();
        var grades = (await Grades.GetAllAsync(new GradeSpec(studentId), asNoTracking: true)).ToList();

        var assignmentsMaxByCourse = assignments
            .GroupBy(a => a.CourseId)
            .ToDictionary(g => g.Key, g => g.ToDictionary(a => a.AssignmentId, a => a.MaxGrade));
        var quizzesMaxByCourse = quizzes
            .GroupBy(q => q.CourseId)
            .ToDictionary(g => g.Key, g => g.ToDictionary(q => q.QuizId, q => q.MaxGrade));
        var gradesByCourse = grades
            .GroupBy(g => g.CourseId)
            .ToDictionary(g => g.Key, g => g.OrderBy(gr => gr.GradedAt).ToList());
        var failedCourseIds = studentCourses
            .Where(sc => sc.Status == StudentCourseStatus.Failed)
            .Select(sc => sc.CourseId)
            .ToHashSet();

        var bylawSettings = student?.Bylaw?.Settings;
        var gradeScales = student?.Bylaw?.GradeScales;
        var minPassingSortOrder = student?.Bylaw?.MinPassingGradeSortOrder;

        if (minPassingSortOrder is not null && gradeScales is not null && gradeScales.Count > 0)
        {
            foreach (var gradeEntry in gradesByCourse)
            {
                var finalGrade = gradeEntry.Value.LastOrDefault(g => g.GradeType == GradeType.Final && g.Status == "Graded");
                if (finalGrade is null) continue;

                var pct = finalGrade.MaxScore > 0
                    ? Math.Round(finalGrade.Score / finalGrade.MaxScore * 100, 0)
                    : 0;
                var letter = ResolveGradeScale(gradeScales, pct);
                var sortOrder = gradeScales
                    .Where(s => s.GradeLetter == letter)
                    .Select(s => s.SortOrder)
                    .FirstOrDefault();

                if (sortOrder > minPassingSortOrder.Value)
                    failedCourseIds.Add(gradeEntry.Key);
            }
        }

        var allCourseWorkWeights = (await CourseWorkWeights.GetAllAsync()).ToDictionary(w => w.CourseId);

        var result = new List<TranscriptCourseDto>();

        foreach (var sc in studentCourses)
        {
            if (!courseDict.TryGetValue(sc.CourseId, out var course))
                continue;

            var courseId = course.CourseId;

            var (assignTotalScore, assignTotalMax) = ComputeAssignmentScores(
                courseId, assignmentsMaxByCourse, studentAssignments);
            var (quizTotalScore, quizTotalMax) = ComputeQuizScores(
                courseId, quizzesMaxByCourse, studentQuizzes);

            gradesByCourse.TryGetValue(courseId, out var courseGrades);
            var midterm = courseGrades?.FirstOrDefault(g => g.GradeType == GradeType.Midterm && g.Status == "Graded");
            var final = courseGrades?.FirstOrDefault(g => g.GradeType == GradeType.Final && g.Status == "Graded");

            var hasCoursework = assignTotalMax > 0 || quizTotalMax > 0 || midterm is not null;

            string courseworkStr = "-";
            string totalGradeStr = "-";
            string letter = "-";

            if (hasCoursework)
            {
                allCourseWorkWeights.TryGetValue(courseId, out var courseWorkWeight);

                if (courseWorkWeight is not null)
                {
                    var (assignmentContrib, quizContrib, midContrib, fnContrib) = CalculateContributions(
                        assignTotalScore, assignTotalMax, quizTotalScore, quizTotalMax,
                        courseWorkWeight.AssignmentWeight, courseWorkWeight.QuizWeight,
                        courseWorkWeight.MidtermWeight, bylawSettings?.FinalExamGrade ?? final?.Weight ?? 0,
                        midterm, final);

                    var coursework = assignmentContrib + quizContrib + midContrib;
                    courseworkStr = Math.Round(coursework, 0).ToString();

                    if (final is not null)
                    {
                        var overall = Math.Round(coursework + fnContrib, 0);
                        var assignQuizW = courseWorkWeight.AssignmentWeight + courseWorkWeight.QuizWeight;
                        var midtermW = courseWorkWeight.MidtermWeight;
                        var finalW = bylawSettings?.FinalExamGrade ?? final?.Weight ?? 0;
                        overall = ApplyBylawGradeRules(bylawSettings, gradeScales, failedCourseIds, courseId,
                            assignQuizW, midtermW, finalW,
                            assignmentContrib + quizContrib, midContrib, fnContrib, overall,
                            out var forcedFail);
                        totalGradeStr = overall.ToString();
                        letter = ResolveGradeScale(gradeScales, overall);
                        if (forcedFail)
                        {
                            letter = gradeScales?.OrderByDescending(s => s.SortOrder).FirstOrDefault()?.GradeLetter ?? "F";
                            failedCourseIds.Add(courseId);
                        }
                    }
                    else
                    {
                        totalGradeStr = "-";
                    }
                }
                else
                {
                    var (assignQuizContrib, midtermContrib, finalContrib) = CalculateWeightedContributions(
                        assignTotalScore, assignTotalMax, quizTotalScore, quizTotalMax, midterm, final);

                    courseworkStr = Math.Round(assignQuizContrib + midtermContrib, 0).ToString();

                    if (final is not null)
                    {
                        var overall = Math.Round(assignQuizContrib + midtermContrib + finalContrib, 0);
                        var midtermWeight = midterm?.Weight ?? 0;
                        var finalWeight = final?.Weight ?? 0;
                        var assignQuizWeight = 100 - midtermWeight - finalWeight;
                        overall = ApplyBylawGradeRules(bylawSettings, gradeScales, failedCourseIds, courseId,
                            assignQuizWeight, midtermWeight, finalWeight,
                            assignQuizContrib, midtermContrib, finalContrib, overall,
                            out var forcedFail);
                        totalGradeStr = overall.ToString();
                        letter = ResolveGradeScale(gradeScales, overall);
                        if (forcedFail)
                        {
                            letter = gradeScales?.OrderByDescending(s => s.SortOrder).FirstOrDefault()?.GradeLetter ?? "F";
                            failedCourseIds.Add(courseId);
                        }
                    }
                else
                {
                    totalGradeStr = "-";
                }
            }
            }

            var level = sc.Level ?? ExtractLevelFromCourseCode(course.CourseCode);

            result.Add(new TranscriptCourseDto
            {
                CourseId = course.CourseId,
                CourseName = course.CourseName,
                CourseNameAr = course.CourseNameAr,
                CourseCode = course.CourseCode!,
                CourseCodeAr = course.CourseCodeAr,
                CreditHours = effectiveCredits.GetValueOrDefault(course.CourseId, course.CreditHours),
                Semester = sc.Semester,
                SemesterAr = SemesterHelper.GetSemesterAr(sc.Semester),
                Level = level,
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

        var spec = new StudentSpec(new CourseQueryParams { StudentId = studentId });
        var studentEntity = await Students.GetByIdAsync(spec);
        return CalculateGpa(courseItemList, studentEntity?.Bylaw?.GradeScales);
    }

    public async Task<double?> UpdateStudentGpaIfCompleteAsync(int studentId)
    {
        var studentCourses = (await StudentCourses.GetAllAsync(new StudentCourseIdsSpec(studentId), asNoTracking: true)).ToList();
        var student = await Students.GetByIdAsync(new StudentSpec(new CourseQueryParams { StudentId = studentId }, lightweight: true));
        if (studentCourses.Count == 0 || student is null) return student?.Gpa;

        var courseIds = studentCourses.Select(sc => sc.CourseId).ToList();

        // Batch load all grading data once — eliminates N+1 round-trips
        var assignments = await Assignments.GetAllAsync(new AssignmentSpec(courseIds), asNoTracking: true);
        var quizzes = await Quizzes.GetAllAsync(new QuizSpec(courseIds), asNoTracking: true);
        var allSubmissions = (await StudentAssignments.GetAllAsync(new StudentAssignmentSpec(studentId, "transcript"), asNoTracking: true)).ToList();
        var allQuizSubmissions = (await StudentQuizzes.GetAllAsync(new StudentQuizSpec(studentId, "transcript"), asNoTracking: true)).ToList();
        var allGrades = (await Grades.GetAllAsync(new GradeSpec(studentId), asNoTracking: true)).ToList();

        var assignmentCourseIds = assignments.ToDictionary(a => a.AssignmentId, a => a.CourseId);
        var quizCourseIds = quizzes.ToDictionary(q => q.QuizId, q => q.CourseId);

        foreach (var sc in studentCourses)
        {
            var hasAssignments = allSubmissions.Any(s =>
                assignmentCourseIds.TryGetValue(s.AssignmentId, out var cid) && cid == sc.CourseId && s.Grade.HasValue);
            var hasQuizzes = allQuizSubmissions.Any(s =>
                quizCourseIds.TryGetValue(s.QuizId, out var cid) && cid == sc.CourseId && s.Score.HasValue);
            var hasMidtermOrFinal = allGrades.Any(g =>
                g.CourseId == sc.CourseId && g.Status == "Graded" &&
                (g.GradeType == GradeType.Midterm || g.GradeType == GradeType.Final));

            if (!hasAssignments && !hasQuizzes && !hasMidtermOrFinal)
                return student.Gpa;
        }

        var gpa = await GetCumulativeGpaAsync(studentId);
        student.Gpa = gpa;
        await _unitOfWork.SaveChangesAsync();

        await UpdateStudentLevelIfPromotedAsync(studentId);

        return gpa;
    }

    public async Task<int> GetCompletedHoursAsync(int studentId)
    {
        var studentCourses = (await StudentCourses.GetAllAsync(new StudentCourseIdsSpec(studentId), asNoTracking: true)).ToList();
        var student = await Students.GetByIdAsync(new StudentSpec(new CourseQueryParams { StudentId = studentId }, lightweight: true));
        if (studentCourses.Count == 0 || student is null) return 0;

        var courseIds = studentCourses.Select(sc => sc.CourseId).ToList();
        var courses = (await Courses.GetAllAsync(new CourseBasicSpec(courseIds), asNoTracking: true))
            .ToDictionary(c => c.CourseId);

        var effectiveCredits = await _bylawService.GetEffectiveCreditHoursAsync(
            student.BylawId ?? 0, student.DepartmentId);

        var gradeScales = student.Bylaw?.GradeScales;
        var minPassingSortOrder = student.Bylaw?.MinPassingGradeSortOrder;
        var settings = student.Bylaw?.Settings;

        var allGrades = await Grades.GetAllAsync(new GradeSpec(studentId), asNoTracking: true);
        var finalGradesByCourse = allGrades
            .Where(g => g.GradeType == GradeType.Final && g.Status == "Graded")
            .GroupBy(g => g.CourseId)
            .ToDictionary(g => g.Key, g => g.First());

        Dictionary<int, decimal>? courseworkPctByCourse = null;
        if (settings?.MinPassingCourseworkGrade.HasValue == true)
        {
            courseworkPctByCourse = await ComputeCourseworkPercentagesAsync(studentId, courseIds, allGrades);
        }

        var completedCourseIds = finalGradesByCourse
            .Where(kvp =>
            {
                if (minPassingSortOrder is null || gradeScales is null || gradeScales.Count == 0)
                    return true;

                var finalGrade = kvp.Value;
                var pct = finalGrade.MaxScore > 0
                    ? Math.Round(finalGrade.Score / finalGrade.MaxScore * 100, 0)
                    : 0;
                var letter = ResolveGradeScale(gradeScales, pct);
                var sortOrder = gradeScales
                    .Where(s => s.GradeLetter == letter)
                    .Select(s => s.SortOrder)
                    .FirstOrDefault();
                if (sortOrder > minPassingSortOrder.Value)
                    return false;

                if (courseworkPctByCourse is not null && courseworkPctByCourse.TryGetValue(kvp.Key, out var cpct))
                {
                    if (cpct < settings!.MinPassingCourseworkGrade!.Value)
                        return false;
                }

                return true;
            })
            .Select(kvp => kvp.Key)
            .ToHashSet();

        return studentCourses
            .Where(sc => completedCourseIds.Contains(sc.CourseId))
            .Sum(sc => effectiveCredits.GetValueOrDefault(sc.CourseId, courses.GetValueOrDefault(sc.CourseId)?.CreditHours ?? 0));
    }

    public async Task<int?> UpdateStudentLevelIfPromotedAsync(int studentId)
    {
        var student = await Students.GetByIdAsync(new StudentSpec(new CourseQueryParams { StudentId = studentId }));
        if (student is null || student.Bylaw is null) throw new BylawNotFoundException(studentId);

        var scales = student.Bylaw.Settings.LevelScales;
        if (scales is null || scales.Count == 0) return null;

        var completedHours = await GetCompletedHoursAsync(studentId);

        var targetLevel = scales
            .Where(ls => completedHours >= ls.MinHours)
            .OrderByDescending(ls => ls.Level)
            .Select(ls => ls.Level)
            .FirstOrDefault();

        if (targetLevel == 0) return student.Level;
        if (student.Level == targetLevel) return student.Level;

        student.Level = targetLevel;
        await _unitOfWork.SaveChangesAsync();

        return targetLevel;
    }

    public async Task<AcademicProgressDto> GetAcademicProgressAsync(int studentId)
    {
        var student = await Students.GetByIdAsync(new StudentSpec(new CourseQueryParams { StudentId = studentId }));
        if (student is null)
            throw new StudentNotFoundException(studentId);

        var bylawSpec = new BylawSpec(student.BylawId ?? 0);
        var bylaw = await _unitOfWork
            .GetRepository<Bylaw, int>()
            .GetByIdAsync(bylawSpec);

        if (bylaw is null)
            return new AcademicProgressDto();

        var data = await LoadAcademicProgressDataAsync(studentId);
        var effectiveCredits = await _bylawService.GetEffectiveCreditHoursAsync(
            student.BylawId ?? 0, student.DepartmentId);

        var completedCourseIds = data.AllGrades
            .Where(g => g.GradeType == GradeType.Final && g.Status == "Graded")
            .Select(g => g.CourseId)
            .Distinct()
            .ToHashSet();

        var gpa = await GetCumulativeGpaAsync(studentId);

        var bucketTypes = new[] {
            (Type: CourseType.GeneralUniversity, Name: "University Requirements", NameAr: "متطلبات الجامعة"),
            (Type: CourseType.Faculty, Name: "Faculty Requirements", NameAr: "متطلبات الكلية"),
            (Type: CourseType.Department, Name: "Department Requirements", NameAr: "متطلبات القسم"),
            (Type: CourseType.Specialization, Name: "Major Requirements", NameAr: "متطلبات التخصص"),
            (Type: CourseType.Elective, Name: "Free Electives", NameAr: "مواد اختيارية")
        };

        var electiveBucketTotalHours = (int)bylaw.ElectiveBuckets
            .Where(eb => eb.DepartmentId is null || eb.DepartmentId == student.DepartmentId)
            .Sum(eb => eb.RequiredCreditHours);

        var grouped = bylaw.BylawCourses
            .Where(bc => bc.Course != null)
            .GroupBy(bc => bc.CourseType);

        var buckets = new List<BylawBucketDto>();

        foreach (var (type, name, nameAr) in bucketTypes)
        {
            var group = grouped.FirstOrDefault(g => g.Key == type)?.ToList() ?? [];
            if (group.Count == 0 && type != CourseType.Elective)
                continue;

            var courses = group.Select(bc =>
            {
                var credit = effectiveCredits.GetValueOrDefault(bc.CourseId, bc.Course?.CreditHours ?? 0);
                return new BucketCourseDto
                {
                    CourseId = bc.CourseId,
                    CourseCode = bc.Course?.CourseCode ?? "",
                    CourseCodeAr = bc.Course?.CourseCodeAr,
                    CourseName = bc.Course?.CourseName ?? "",
                    CourseNameAr = bc.Course?.CourseNameAr,
                    CreditHours = credit,
                    IsCompleted = completedCourseIds.Contains(bc.CourseId)
                };
            }).ToList();

            var completedHours = courses.Where(c => c.IsCompleted).Sum(c => c.CreditHours);
            var requiredHours = courses.Sum(c => c.CreditHours);

            if (type == CourseType.Elective && electiveBucketTotalHours > 0)
            {
                requiredHours = electiveBucketTotalHours;
            }

            buckets.Add(new BylawBucketDto
            {
                BucketName = name,
                BucketNameAr = nameAr,
                BucketType = type.ToString(),
                CompletedHours = completedHours,
                RequiredHours = requiredHours,
                Courses = courses
            });
        }

        var graduationHours = bylaw.Settings?.TotalHoursToCompleteDegree
            ?? buckets.Sum(b => b.RequiredHours);

        var totalCompletedHours = buckets.Sum(b => b.CompletedHours);
        var minPassingGpa = bylaw.MinPassingGpa;
        var meetsMinPassingGpa = minPassingGpa is null || gpa >= (double)minPassingGpa.Value;
        var meetsTotalHourRequirement = totalCompletedHours >= graduationHours;

        return new AcademicProgressDto
        {
            TotalCompletedHours = totalCompletedHours,
            TotalRequiredHours = buckets.Sum(b => b.RequiredHours),
            TotalGraduationHours = graduationHours,
            Gpa = gpa,
            MinPassingGpa = minPassingGpa is decimal d ? (double)d : null,
            MeetsMinPassingGpa = meetsMinPassingGpa,
            MeetsTotalHourRequirement = meetsTotalHourRequirement,
            IsEligibleForGraduation = meetsMinPassingGpa && meetsTotalHourRequirement,
            MinPassingGradeLetter = bylaw.MinPassingGradeLetter,
            Buckets = buckets
        };
    }

    private async Task<(
        List<StudentCourse> StudentCourses,
        List<Course> Courses,
        List<Grade> AllGrades,
        List<StudentAssignment> Submissions,
        List<StudentQuiz> QuizSubmissions
    )> LoadAcademicProgressDataAsync(int studentId)
    {
        var studentCourses = (await StudentCourses.GetAllAsync(
            new StudentCourseIdsSpec(studentId), asNoTracking: true)).ToList();
        var courseIds = studentCourses.Select(sc => sc.CourseId).ToList();
        var courses = courseIds.Count > 0
            ? (await Courses.GetAllAsync(new CourseBasicSpec(courseIds), asNoTracking: true)).ToList()
            : new List<Course>();
        var allGrades = (await Grades.GetAllAsync(new GradeSpec(studentId), asNoTracking: true)).ToList();
        var submissions = (await StudentAssignments.GetAllAsync(
            new StudentAssignmentSpec(studentId, byStudent: true, dummy: true), asNoTracking: true)).ToList();
        var quizSubmissions = (await StudentQuizzes.GetAllAsync(
            new StudentQuizSpec(studentId, true, true), asNoTracking: true)).ToList();
        return (studentCourses, courses, allGrades, submissions, quizSubmissions);
    }

    public async Task<byte[]> ExportTranscriptPdfAsync(int studentId)
    {
        var spec = new StudentSpec(new CourseQueryParams { StudentId = studentId });
        var studentEntity = await Students.GetByIdAsync(spec);
        if (studentEntity is null)
            throw new StudentNotFoundException(studentId);

        var courseDtos = await GetTranscriptAsync(studentId);

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
            c.CourseId,
            c.Semester
        )).ToList();

        int totalCredits = courseItemList.Sum(c => c.Item.CreditHours);
        double gpa = CalculateGpa(courseItemList.Select(c => c.Item).ToList(), studentEntity?.Bylaw?.GradeScales);

        var semesterGroups = courseItemList
            .GroupBy(c => !string.IsNullOrEmpty(c.Semester) ? c.Semester : "Courses")
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
            StudentName = studentEntity?.User?.FullName ?? "",
            StudentCode = studentEntity?.StudentCode ?? "-",
            Faculty = studentEntity?.User?.Faculty?.FacultyName,
            Level = studentEntity?.Level,
            Department = studentEntity?.Department?.DepartmentName,
            TotalCredits = totalCredits,
            GPA = gpa,
            Semesters = semesters
        };

        return _pdfExportService.ExportTranscript(dto);
    }

    // Instructor (read-only)

    public async Task<IEnumerable<GradeDto>> GetByStudentAndCourseAsync(int instructorId, int studentId, int courseId)
    {
        var student = await Students.GetByIdAsync(studentId);
        if (student is null)
            throw new StudentNotFoundException(studentId);

        var course = await Courses.GetByIdAsync(courseId);
        if (course is null)
            throw new CourseNotFoundException(courseId);

        var teaches = await Classes.AnyAsync(c => c.CourseId == courseId && c.InstructorId == instructorId);
        if (!teaches)
            throw new InvalidOperationException("Not authorized.");

        var result = new List<GradeDto>();

        // Assignment grades
        var assignments = await Assignments.GetAllAsync(new AssignmentSpec(courseId, byCourse: true), asNoTracking: true);
        var assignmentIds = assignments.Select(a => a.AssignmentId).ToHashSet();

        var submissions = (await StudentAssignments.GetAllAsync(new StudentAssignmentSpec(studentId, byStudent: true, dummy: true), asNoTracking: true))
            .Where(sa => assignmentIds.Contains(sa.AssignmentId) && sa.Grade.HasValue)
            .ToList();

        foreach (var sa in submissions)
        {
            var assignment = assignments.FirstOrDefault(a => a.AssignmentId == sa.AssignmentId);
            if (assignment is null) continue;
            result.Add(new GradeDto
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
                GradedAt = (sa.GradedAt ?? EgyptTime.Now).ToString("dd MM yyyy HH:mm"),
                Notes = sa.Feedback
            });
        }

        // Quiz grades
        var quizzes = await Quizzes.GetAllAsync(new QuizSpec(courseId, byCourse: true), asNoTracking: true);
        var quizIds = quizzes.Select(q => q.QuizId).ToHashSet();

        var quizSubmissions = (await StudentQuizzes.GetAllAsync(new StudentQuizSpec(studentId, true, true), asNoTracking: true))
            .Where(sq => quizIds.Contains(sq.QuizId) && sq.Score.HasValue)
            .ToList();

        foreach (var sq in quizSubmissions)
        {
            var quiz = quizzes.FirstOrDefault(q => q.QuizId == sq.QuizId);
            if (quiz is null) continue;
            result.Add(new GradeDto
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
            });
        }

        return result;
    }

    public async Task<InstructorCourseGradesDto> GetCourseGradesOverviewAsync(int courseId, int instructorId)
    {
        var course = await Courses.GetByIdAsync(courseId);
        if (course is null)
            throw new CourseNotFoundException(courseId);

        var teaches = await Classes.AnyAsync(c => c.CourseId == courseId && c.InstructorId == instructorId);
        if (!teaches)
            throw new InvalidOperationException("Not authorized.");

        var data = await LoadCourseGradesOverviewDataAsync(courseId);
        var assessmentMap = BuildAssessmentMap(data.Assignments, data.Quizzes, data.CourseGrades);
        var courseWorkWeight = await CourseWorkWeights.GetByIdAsync(courseId);

        var allOverallPercents = new List<double>();
        var passCount = 0;
        var studentGrades = new List<InstructorStudentGradeDto>();

        foreach (var student in data.EnrolledStudents)
        {
            var dto = ProcessStudentGrade(student,
                data.CourseAssignmentSubmissions, data.CourseQuizSubmissions, data.CourseGrades,
                data.AssignmentsById, data.QuizzesById, data.FailedCourseStudentIds,
                courseId, ref assessmentMap, courseWorkWeight);
            allOverallPercents.Add(dto.OverallPercent);
            if (dto.Letter != "-" && dto.Letter != "F" && dto.Letter != "Con" && dto.Letter != "W" && dto.Letter != "I")
                passCount++;
            studentGrades.Add(dto);
        }

        var (assessmentsList, totalCoursework, avgPercent, passRate) = BuildAssessmentSummary(
            allOverallPercents, passCount, assessmentMap);

        var gradedCount = data.GradedAssignmentCount + data.GradedQuizCount + data.GradedMidtermCount + data.GradedFinalCount;

        return new InstructorCourseGradesDto
        {
            CourseId = courseId,
            CourseName = course.CourseName,
            CourseCode = course.CourseCode,
            Summary = new InstructorCourseSummaryDto
            {
                AveragePercent = Math.Round(avgPercent, 1),
                PassRate = Math.Round(passRate, 1),
                TotalStudents = data.EnrolledStudents.Count,
                GradedAssessmentsCount = gradedCount,
                AverageCoursework = Math.Round(avgPercent, 1),
                TotalCoursework = Math.Round(totalCoursework, 1),
                GradedAssessments = gradedCount,
                TotalAssessments = assessmentsList.Count
            },
            Assessments = assessmentsList,
            Students = studentGrades
        };
    }

    private async Task<(
        List<Assignment> Assignments,
        Dictionary<int, Assignment> AssignmentsById,
        List<Quiz> Quizzes,
        Dictionary<int, Quiz> QuizzesById,
        List<Student> EnrolledStudents,
        HashSet<int> FailedCourseStudentIds,
        List<StudentAssignment> CourseAssignmentSubmissions,
        List<StudentQuiz> CourseQuizSubmissions,
        List<Grade> CourseGrades,
        int GradedAssignmentCount,
        int GradedQuizCount,
        int GradedMidtermCount,
        int GradedFinalCount
    )> LoadCourseGradesOverviewDataAsync(int courseId)
    {
        var assignments = (await Assignments.GetAllAsync(new AssignmentSpec(courseId, byCourse: true), asNoTracking: true)).ToList();
        var assignmentIds = assignments.Select(a => a.AssignmentId).ToHashSet();
        var assignmentsById = assignments.ToDictionary(a => a.AssignmentId);
        var quizzes = (await Quizzes.GetAllAsync(new QuizSpec(courseId, byCourse: true), asNoTracking: true)).ToList();
        var quizIds = quizzes.Select(q => q.QuizId).ToHashSet();
        var quizzesById = quizzes.ToDictionary(q => q.QuizId);

        var studentCourses = (await StudentCourses.GetAllAsync(new StudentCourseIdsSpec(courseId, true, StudentCourseStatus.InProgress), asNoTracking: true)).ToList();
        var enrolledStudentIds = studentCourses
            .Select(sc => sc.StudentId)
            .ToHashSet();

        var enrolledStudents = (await Students.GetAllAsync(new StudentSpec(enrolledStudentIds.ToList()), asNoTracking: true))
            .OrderBy(s => s.User.FullName)
            .ToList();

        var courseAssignmentSubmissions = (await StudentAssignments.GetAllAsync(new StudentAssignmentSpec(assignmentIds, true), asNoTracking: true)).ToList();
        var courseQuizSubmissions = (await StudentQuizzes.GetAllAsync(new StudentQuizSpec(quizIds, true), asNoTracking: true)).ToList();
        var courseGrades = (await Grades.GetAllAsync(new GradeSpec(courseId, true), asNoTracking: true)).ToList();

        var gradedAssignmentCount = courseAssignmentSubmissions.Count(sa => sa.Grade.HasValue);
        var gradedQuizCount = courseQuizSubmissions.Count(sq => sq.Score.HasValue);
        var gradedMidtermCount = courseGrades.Count(g => g.GradeType == GradeType.Midterm && g.Status == "Graded");
        var gradedFinalCount = courseGrades.Count(g => g.GradeType == GradeType.Final && g.Status == "Graded");

        var failedCourseStudentIds = (await StudentCourses.GetAllAsync(
            new StudentCourseIdsSpec(enrolledStudentIds.ToList(), StudentCourseStatus.Failed), asNoTracking: true))
            .Select(f => f.StudentId)
            .ToHashSet();

        return (
            Assignments: assignments,
            AssignmentsById: assignmentsById,
            Quizzes: quizzes,
            QuizzesById: quizzesById,
            EnrolledStudents: enrolledStudents,
            FailedCourseStudentIds: failedCourseStudentIds,
            CourseAssignmentSubmissions: courseAssignmentSubmissions,
            CourseQuizSubmissions: courseQuizSubmissions,
            CourseGrades: courseGrades,
            GradedAssignmentCount: gradedAssignmentCount,
            GradedQuizCount: gradedQuizCount,
            GradedMidtermCount: gradedMidtermCount,
            GradedFinalCount: gradedFinalCount
        );
    }

    private Dictionary<string, (List<double> Percents, int Count, string Type, double MaxScore, int Id)> BuildAssessmentMap(
        List<Assignment> assignments,
        List<Quiz> quizzes,
        List<Grade> courseGrades)
    {
        var assessmentMap = new Dictionary<string, (List<double> Percents, int Count, string Type, double MaxScore, int Id)>();

        foreach (var a in assignments)
        {
            var key = a.Title.ToLowerInvariant();
            assessmentMap.TryAdd(key, (new List<double>(), 0, GradeType.Assignment.ToString(), (double)a.MaxGrade, a.AssignmentId));
        }
        foreach (var q in quizzes)
        {
            var key = q.Title.ToLowerInvariant();
            assessmentMap.TryAdd(key, (new List<double>(), 0, GradeType.Quiz.ToString(), (double)q.MaxGrade, q.QuizId));
        }

        var midtermGrades = courseGrades.Where(g => g.GradeType == GradeType.Midterm).ToList();
        var finalGrades = courseGrades.Where(g => g.GradeType == GradeType.Final).ToList();

        foreach (var g in midtermGrades)
        {
            var key = GradeType.Midterm.ToString().ToLowerInvariant();
            if (!assessmentMap.ContainsKey(key))
                assessmentMap[key] = (new List<double>(), 0, GradeType.Midterm.ToString(), (double)g.MaxScore, -1);
        }
        foreach (var g in finalGrades)
        {
            var key = GradeType.Final.ToString().ToLowerInvariant();
            if (!assessmentMap.ContainsKey(key))
                assessmentMap[key] = (new List<double>(), 0, GradeType.Final.ToString(), (double)g.MaxScore, -2);
        }

        return assessmentMap;
    }

    private InstructorStudentGradeDto ProcessStudentGrade(
        Student student,
        List<StudentAssignment> courseAssignmentSubmissions,
        List<StudentQuiz> courseQuizSubmissions,
        List<Grade> courseGrades,
        Dictionary<int, Assignment> assignmentsById,
        Dictionary<int, Quiz> quizzesById,
        HashSet<int> failedCourseStudentIds,
        int courseId,
        ref Dictionary<string, (List<double> Percents, int Count, string Type, double MaxScore, int Id)> assessmentMap,
        CourseWorkWeight? courseWorkWeight = null)
    {
        var (assignTotalScore, assignTotalMax) = (0m, 0m);
        foreach (var sa in courseAssignmentSubmissions)
        {
            if (sa.StudentId == student.UserId && sa.Grade.HasValue && assignmentsById.TryGetValue(sa.AssignmentId, out var a))
            {
                assignTotalScore += sa.Grade.Value;
                assignTotalMax += a.MaxGrade;
            }
        }

        var (quizTotalScore, quizTotalMax) = (0m, 0m);
        foreach (var sq in courseQuizSubmissions)
        {
            if (sq.StudentId == student.UserId && sq.Score.HasValue && quizzesById.TryGetValue(sq.QuizId, out var q))
            {
                quizTotalScore += sq.Score.Value;
                quizTotalMax += q.MaxGrade;
            }
        }

        var studentCourseGrades = courseGrades
            .Where(g => g.StudentId == student.UserId)
            .ToList();
        var midterm = studentCourseGrades.FirstOrDefault(g => g.GradeType == GradeType.Midterm && g.Status == "Graded");
        var final = studentCourseGrades.FirstOrDefault(g => g.GradeType == GradeType.Final && g.Status == "Graded");

        var assessments = new List<InstructorAssessmentDto>();

        var studentSubmissions = courseAssignmentSubmissions
            .Where(sa => sa.StudentId == student.UserId && sa.Grade.HasValue)
            .ToList();

        var midtermDisplayWeight = courseWorkWeight?.MidtermWeight ?? midterm?.Weight ?? 0;
        var finalDisplayWeight = student.Bylaw?.Settings?.FinalExamGrade ?? final?.Weight ?? 0;

        foreach (var sa in studentSubmissions)
        {
            var assignment = assignmentsById[sa.AssignmentId];
            var key = assignment.Title.ToLowerInvariant();
            var pct = assignment.MaxGrade > 0 ? (double)(sa.Grade!.Value / assignment.MaxGrade * 100) : 0;
            assessments.Add(new InstructorAssessmentDto
            {
                AssessmentId = assignment.AssignmentId,
                Name = assignment.Title,
                Type = GradeType.Assignment.ToString(),
                Score = (double)sa.Grade!.Value,
                MaxScore = (double)assignment.MaxGrade,
                Weight = (double)assignment.MaxGrade,
                Percent = pct
            });
            if (assessmentMap.TryGetValue(key, out var entry))
            {
                entry.Percents.Add(pct);
                assessmentMap[key] = (entry.Percents, entry.Count + 1, entry.Type, entry.MaxScore, entry.Id);
            }
        }

        var studentQuizSubmissions = courseQuizSubmissions
            .Where(sq => sq.StudentId == student.UserId && sq.Score.HasValue)
            .ToList();

        foreach (var sq in studentQuizSubmissions)
        {
            var quiz = quizzesById[sq.QuizId];
            var key = quiz.Title.ToLowerInvariant();
            var pct = quiz.MaxGrade > 0 ? (double)(sq.Score!.Value / quiz.MaxGrade * 100) : 0;
            assessments.Add(new InstructorAssessmentDto
            {
                AssessmentId = quiz.QuizId,
                Name = quiz.Title,
                Type = GradeType.Quiz.ToString(),
                Score = (double)sq.Score!.Value,
                MaxScore = (double)quiz.MaxGrade,
                Weight = (double)quiz.MaxGrade,
                Percent = pct
            });
            if (assessmentMap.TryGetValue(key, out var entry))
            {
                entry.Percents.Add(pct);
                assessmentMap[key] = (entry.Percents, entry.Count + 1, entry.Type, entry.MaxScore, entry.Id);
            }
        }

        if (midterm is not null)
        {
            var key = GradeType.Midterm.ToString().ToLowerInvariant();
            var pct = midterm.MaxScore > 0 ? (double)(midterm.Score / midterm.MaxScore * 100) : 0;
            assessments.Add(new InstructorAssessmentDto
            {
                AssessmentId = -1,
                Name = midterm.Title,
                Type = GradeType.Midterm.ToString(),
                Score = (double)midterm.Score,
                MaxScore = (double)midterm.MaxScore,
                Weight = (double)midtermDisplayWeight,
                Percent = pct
            });
            if (assessmentMap.TryGetValue(key, out var entry))
            {
                entry.Percents.Add(pct);
                assessmentMap[key] = (entry.Percents, entry.Count + 1, entry.Type, entry.MaxScore, entry.Id);
            }
        }

        if (final is not null)
        {
            var key = GradeType.Final.ToString().ToLowerInvariant();
            var pct = final.MaxScore > 0 ? (double)(final.Score / final.MaxScore * 100) : 0;
            assessments.Add(new InstructorAssessmentDto
            {
                AssessmentId = -2,
                Name = final.Title,
                Type = GradeType.Final.ToString(),
                Score = (double)final.Score,
                MaxScore = (double)final.MaxScore,
                Weight = (double)finalDisplayWeight,
                Percent = pct
            });
            if (assessmentMap.TryGetValue(key, out var entry))
            {
                entry.Percents.Add(pct);
                assessmentMap[key] = (entry.Percents, entry.Count + 1, entry.Type, entry.MaxScore, entry.Id);
            }
        }

        decimal overallDecimal;
        decimal midtermWeight;
        decimal finalWeight;
        decimal assignQuizWeight;
        decimal assignQuizContrib;
        decimal midtermContrib;
        decimal finalContrib;

        var bylawSettingsForWeights = student.Bylaw?.Settings;
        if (courseWorkWeight is not null)
        {
            var (aContrib, qContrib, mContrib, fContrib) = CalculateContributions(
                assignTotalScore, assignTotalMax, quizTotalScore, quizTotalMax,
                courseWorkWeight.AssignmentWeight, courseWorkWeight.QuizWeight,
                courseWorkWeight.MidtermWeight, bylawSettingsForWeights?.FinalExamGrade ?? final?.Weight ?? 0,
                midterm, final);

            assignQuizContrib = aContrib + qContrib;
            midtermContrib = mContrib;
            finalContrib = fContrib;
            overallDecimal = assignQuizContrib + midtermContrib + finalContrib;
            midtermWeight = courseWorkWeight.MidtermWeight;
            finalWeight = bylawSettingsForWeights?.FinalExamGrade ?? final?.Weight ?? 0;
            assignQuizWeight = courseWorkWeight.AssignmentWeight + courseWorkWeight.QuizWeight;
        }
        else
        {
            (assignQuizContrib, midtermContrib, finalContrib) = CalculateWeightedContributions(
                assignTotalScore, assignTotalMax, quizTotalScore, quizTotalMax, midterm, final);

            overallDecimal = assignQuizContrib + midtermContrib + finalContrib;
            midtermWeight = midterm?.Weight ?? 0;
            finalWeight = final?.Weight ?? 0;
            assignQuizWeight = 100 - midtermWeight - finalWeight;
        }

        var isForcedFailing = false;
        if (student.Bylaw is not null)
        {
            overallDecimal = ApplyBylawGradeRules(
                student.Bylaw.Settings, student.Bylaw.GradeScales,
                failedCourseStudentIds, courseId,
                assignQuizWeight, midtermWeight, finalWeight,
                assignQuizContrib, midtermContrib, finalContrib, overallDecimal,
                out isForcedFailing);
        }

        var overallPercent = Math.Round((double)overallDecimal, 0);

        var letter = ResolveGradeScale(student.Bylaw?.GradeScales, overallDecimal);
        if (isForcedFailing)
            letter = student.Bylaw?.GradeScales?.OrderByDescending(s => s.SortOrder).FirstOrDefault()?.GradeLetter ?? "F";

        return new InstructorStudentGradeDto
        {
            StudentId = student.UserId,
            StudentCode = student.StudentCode ?? "",
            FullName = student.User.FullName,
            Assessments = assessments,
            OverallPercent = overallPercent,
            Letter = letter
        };
    }

    private (List<InstructorAssessmentSummaryDto> Assessments, double TotalCoursework, double AvgPercent, double PassRate) BuildAssessmentSummary(
        List<double> allOverallPercents,
        int passCount,
        Dictionary<string, (List<double> Percents, int Count, string Type, double MaxScore, int Id)> assessmentMap)
    {
        var avgPercent = allOverallPercents.Count > 0 ? allOverallPercents.Average() : 0;
        var passRate = allOverallPercents.Count > 0 ? (double)passCount / allOverallPercents.Count * 100 : 0;

        var assessmentsList = new List<InstructorAssessmentSummaryDto>();
        double totalCoursework = 0;
        foreach (var kvp in assessmentMap)
        {
            var (percents, count, type, maxScore, id) = kvp.Value;
            var avg = percents.Count > 0 ? percents.Average() : (double?)null;
            var title = type == GradeType.Midterm.ToString() ? "Midterm Exam"
                       : type == GradeType.Final.ToString() ? "Final Exam"
                       : kvp.Key;
            assessmentsList.Add(new InstructorAssessmentSummaryDto
            {
                Id = id,
                Title = title,
                Type = type,
                MaxScore = maxScore,
                Average = avg.HasValue ? Math.Round(avg.Value, 1) : null,
                Submissions = count
            });
            totalCoursework += maxScore;
        }

        return (assessmentsList, totalCoursework, avgPercent, passRate);
    }

    // Complaints

    public async Task<GradeComplaintResponseDto> FileComplaintAsync(int studentId, GradeComplaintDto dto)
    {
        var title = dto.ComplaintType.ToLowerInvariant() switch
        {
            "assignment" => await ValidateAssignmentComplaint(studentId, dto.GradeId),
            "quiz" => await ValidateQuizComplaint(studentId, dto.GradeId),
            "midterm" or "final" or "project" => await ValidateGradeComplaint(studentId, dto.GradeId, dto.ComplaintType),
            _ => throw new InvalidOperationException($"Unknown complaint type: {dto.ComplaintType}")
        };

        var complaintCourseId = await ResolveComplaintCourseIdAsync(dto.GradeId, dto.ComplaintType);
        if (complaintCourseId.HasValue)
        {
            await EnsureCourseActiveAsync(complaintCourseId.Value);
            await EnsureStudentEnrollmentActiveAsync(studentId, complaintCourseId!.Value);
        }

        var alreadyFiled = await Complaints.AnyAsync(c => c.GradeId == dto.GradeId && c.StudentId == studentId && c.Status == ComplaintStatus.Pending);
        if (alreadyFiled)
            throw new InvalidOperationException("You already have a pending complaint for this grade.");

        var complaint = new GradeComplaint
        {
            GradeId = dto.GradeId,
            StudentId = studentId,
            ComplaintType = dto.ComplaintType,
            Details = dto.Details,
            Status = ComplaintStatus.Pending,
            SubmittedAt = EgyptTime.Now
        };

        Complaints.Add(complaint);
        await _unitOfWork.SaveChangesAsync();

        return MapComplaintToDto(complaint, title);
    }

    public async Task<IEnumerable<GradeComplaintResponseDto>> GetComplaintsAsync(int studentId)
    {
        var spec = new GradeComplaintSpec(studentId);
        var complaints = await Complaints.GetAllAsync(spec, asNoTracking: true);

        // Batch resolve titles
        var titleByComplaint = await BatchResolveTitlesAsync(complaints);

        var result = new List<GradeComplaintResponseDto>();
        foreach (var c in complaints)
        {
            var title = titleByComplaint.TryGetValue(c.ComplaintId, out var t) ? t : "";
            result.Add(MapComplaintToDto(c, title));
        }

        return result;
    }

    public async Task<GradeComplaintResponseDto?> ReviewComplaintAsync(int complaintId, int instructorId)
    {
        var complaint = await Complaints.GetByIdAsync(complaintId);
        if (complaint is null)
            throw new ComplaintNotFoundException(complaintId);

        var courseId = await ResolveComplaintCourseIdAsync(complaint.GradeId, complaint.ComplaintType);
        if (courseId is null)
            throw new InvalidOperationException("Could not resolve complaint course.");

        var teachesCourse = await Classes.AnyAsync(c => c.CourseId == courseId && c.InstructorId == instructorId);
        if (!teachesCourse)
            throw new InvalidOperationException("Not authorized.");

        complaint.Status = ComplaintStatus.Resolved;
        Complaints.Update(complaint);
        await _unitOfWork.SaveChangesAsync();

        var title = await ResolveComplaintTitleAsync(complaint.GradeId, complaint.ComplaintType);

        await _notificationService.SendAsync(
            complaint.StudentId,
            NotificationType.GradeComplaintReviewed,
            $"Your grade complaint for '{title}' has been reviewed by your instructor.",
            clickUrl: $"/courses/{courseId}/grades");

        return MapComplaintToDto(complaint, title);
    }

    private async Task<int?> ResolveComplaintCourseIdAsync(int gradeId, string complaintType)
    {
        return complaintType.ToLowerInvariant() switch
        {
            "assignment" => await ResolveAssignmentCourseId(gradeId),
            "quiz" => await ResolveQuizCourseId(gradeId),
            _ => await ResolveGradeCourseId(gradeId)
        };
    }

    private async Task<int?> ResolveAssignmentCourseId(int gradeId)
    {
        var submission = await StudentAssignments.GetByIdAsync(gradeId);
        if (submission is null) return null;
        var assignment = await Assignments.GetByIdAsync(submission.AssignmentId);
        return assignment?.CourseId;
    }

    private async Task<int?> ResolveQuizCourseId(int quizId)
    {
        var quiz = await Quizzes.GetByIdAsync(quizId);
        return quiz?.CourseId;
    }

    private async Task<int?> ResolveGradeCourseId(int gradeId)
    {
        var grade = await Grades.GetByIdAsync(gradeId);
        return grade?.CourseId;
    }

    public async Task<IEnumerable<InstructorGradeComplaintDto>> GetCourseComplaintsAsync(int courseId, int instructorId)
    {
        var teaches = await Classes.AnyAsync(c => c.CourseId == courseId && c.InstructorId == instructorId);
        if (!teaches)
            throw new InvalidOperationException("Not authorized.");

        var data = await LoadComplaintsDataAsync(courseId);

        var gradeTitles = data.Grades.ToDictionary(g => g.GradeId, g => g.Title);
        var result = new List<InstructorGradeComplaintDto>();

        foreach (var c in data.AssignmentComplaints)
        {
            result.Add(new InstructorGradeComplaintDto
            {
                Id = c.ComplaintId,
                StudentName = data.StudentMap.GetValueOrDefault(c.StudentId, "Unknown"),
                ComplaintType = c.ComplaintType,
                AssessmentTitle = data.AssignTitles.GetValueOrDefault(c.GradeId, ""),
                Reason = c.Details,
                Status = c.Status.ToString().ToLower(),
                CreatedAt = c.SubmittedAt.ToString("o"),
                InstructorResponse = c.InstructorResponse
            });
        }

        foreach (var c in data.QuizComplaints)
        {
            result.Add(new InstructorGradeComplaintDto
            {
                Id = c.ComplaintId,
                StudentName = data.StudentMap.GetValueOrDefault(c.StudentId, "Unknown"),
                ComplaintType = c.ComplaintType,
                AssessmentTitle = data.QuizTitles.GetValueOrDefault(c.GradeId, ""),
                Reason = c.Details,
                Status = c.Status.ToString().ToLower(),
                CreatedAt = c.SubmittedAt.ToString("o"),
                InstructorResponse = c.InstructorResponse
            });
        }

        foreach (var c in data.GradeComplaints)
        {
            result.Add(new InstructorGradeComplaintDto
            {
                Id = c.ComplaintId,
                StudentName = data.StudentMap.GetValueOrDefault(c.StudentId, "Unknown"),
                ComplaintType = c.ComplaintType,
                AssessmentTitle = gradeTitles.GetValueOrDefault(c.GradeId, ""),
                Reason = c.Details,
                Status = c.Status.ToString().ToLower(),
                CreatedAt = c.SubmittedAt.ToString("o"),
                InstructorResponse = c.InstructorResponse
            });
        }

        return result;
    }

    private async Task<(
        List<GradeComplaint> AssignmentComplaints,
        List<GradeComplaint> QuizComplaints,
        List<GradeComplaint> GradeComplaints,
        Dictionary<int, string> StudentMap,
        List<StudentAssignment> Submissions,
        Dictionary<int, string> AssignTitles,
        List<StudentQuiz> QuizSubmissions,
        Dictionary<int, string> QuizTitles,
        List<Grade> Grades
    )> LoadComplaintsDataAsync(int courseId)
    {
        var allComplaints = await Complaints.GetAllAsync(
            new GradeComplaintSpec(courseId, byCourse: true, unused: true), asNoTracking: true);

        var allStudentIds = new HashSet<int>();
        var rawAssignmentComplaints = new List<GradeComplaint>();
        var rawQuizComplaints = new List<GradeComplaint>();
        var rawGradeComplaints = new List<GradeComplaint>();

        foreach (var c in allComplaints)
        {
            allStudentIds.Add(c.StudentId);
            switch (c.ComplaintType.ToLowerInvariant())
            {
                case "assignment": rawAssignmentComplaints.Add(c); break;
                case "quiz": rawQuizComplaints.Add(c); break;
                default: rawGradeComplaints.Add(c); break;
            }
        }

        var studentMap = (await Students.GetAllAsync(new StudentSpec(allStudentIds.ToList(), lightweight: true), asNoTracking: true))
            .ToDictionary(s => s.UserId, s => s.User.FullName);

        // --- Filter assignment complaints by course ---
        var assignmentComplaints = new List<GradeComplaint>();
        var submissions = new List<StudentAssignment>();
        var assignTitles = new Dictionary<int, string>();
        var assignmentPks = rawAssignmentComplaints.Select(c => c.GradeId).ToList();
        if (assignmentPks.Count > 0)
        {
            submissions = (await StudentAssignments.GetAllAsync(
                new StudentAssignmentSpec(assignmentPks, "batch"), asNoTracking: true)).ToList();
            var assignIds = submissions.Select(s => s.AssignmentId).ToHashSet();
            var assignments = await Assignments.GetAllAsync(new AssignmentSpec(assignIds.ToList(), byIds: true), asNoTracking: true);
            var assignById = assignments.ToDictionary(a => a.AssignmentId);
            var courseAssignIds = assignments.Where(a => a.CourseId == courseId).Select(a => a.AssignmentId).ToHashSet();
            foreach (var s in submissions)
            {
                if (!courseAssignIds.Contains(s.AssignmentId)) continue;
                if (assignById.TryGetValue(s.AssignmentId, out var a))
                    assignTitles[s.StudentAssignmentId] = a.Title;
            }
            var validPks = assignTitles.Keys.ToHashSet();
            assignmentComplaints = rawAssignmentComplaints.Where(c => validPks.Contains(c.GradeId)).ToList();
        }

        // --- Filter quiz complaints by course ---
        var quizComplaints = new List<GradeComplaint>();
        var quizTitles = new Dictionary<int, string>();
        var quizPks = rawQuizComplaints.Select(c => c.GradeId).ToList();
        if (quizPks.Count > 0)
        {
            var quizzes = await Quizzes.GetAllAsync(new QuizSpec(quizPks, byIds: true), asNoTracking: true);
            var courseQuizIds = quizzes.Where(q => q.CourseId == courseId).Select(q => q.QuizId).ToHashSet();
            foreach (var q in quizzes.Where(q => q.CourseId == courseId))
                quizTitles[q.QuizId] = q.Title;
            quizComplaints = rawQuizComplaints.Where(c => courseQuizIds.Contains(c.GradeId)).ToList();
        }

        // --- Filter grade complaints by course ---
        var gradeComplaints = new List<GradeComplaint>();
        var grades = new List<Grade>();
        var gradePks = rawGradeComplaints.Select(c => c.GradeId).ToList();
        if (gradePks.Count > 0)
        {
            grades = (await Grades.GetAllAsync(new GradeSpec(gradePks, true), asNoTracking: true))
                .Where(g => g.CourseId == courseId).ToList();
            var validGradeIds = grades.Select(g => g.GradeId).ToHashSet();
            gradeComplaints = rawGradeComplaints.Where(c => validGradeIds.Contains(c.GradeId)).ToList();
        }

        var quizSubmissions = new List<StudentQuiz>();

        return (assignmentComplaints, quizComplaints, gradeComplaints,
                studentMap, submissions, assignTitles, quizSubmissions, quizTitles, grades);
    }

    private async Task<bool> BelongsToCourse(int gradeId, string complaintType, int courseId)
    {
        return complaintType.ToLowerInvariant() switch
        {
            "assignment" => await BelongsToCourseAssignment(gradeId, courseId),
            "quiz" => await BelongsToCourseQuiz(gradeId, courseId),
            _ => await BelongsToCourseGrade(gradeId, courseId)
        };
    }

    private async Task<bool> BelongsToCourseAssignment(int gradeId, int courseId)
    {
        var submission = await StudentAssignments.GetByIdAsync(gradeId);
        if (submission is null) return false;
        var assignment = await Assignments.GetByIdAsync(submission.AssignmentId);
        return assignment?.CourseId == courseId;
    }

    private async Task<bool> BelongsToCourseQuiz(int quizId, int courseId)
    {
        var quiz = await Quizzes.GetByIdAsync(quizId);
        return quiz?.CourseId == courseId;
    }

    private async Task<bool> BelongsToCourseGrade(int gradeId, int courseId)
    {
        var grade = await Grades.GetByIdAsync(gradeId);
        return grade?.CourseId == courseId;
    }

    public async Task<GradeComplaintResponseDto?> UpdateComplaintStatusAsync(int complaintId, int instructorId, ReviewComplaintDto dto)
    {
        var complaint = await Complaints.GetByIdAsync(complaintId);
        if (complaint is null)
            throw new ComplaintNotFoundException(complaintId);

        var courseId = await ResolveComplaintCourseIdAsync(complaint.GradeId, complaint.ComplaintType);
        if (courseId is null)
            throw new InvalidOperationException("Could not resolve complaint course.");

        var teachesCourse = await Classes.AnyAsync(c => c.CourseId == courseId && c.InstructorId == instructorId);
        if (!teachesCourse)
            throw new InvalidOperationException("Not authorized.");

        complaint.Status = (ComplaintStatus)Enum.Parse(typeof(ComplaintStatus), dto.Status, ignoreCase: true);
        complaint.InstructorResponse = dto.InstructorResponse;
        Complaints.Update(complaint);
        await _unitOfWork.SaveChangesAsync();

        var title = await ResolveComplaintTitleAsync(complaint.GradeId, complaint.ComplaintType);

        await _notificationService.SendAsync(
            complaint.StudentId,
            NotificationType.GradeComplaintReviewed,
            $"Your grade complaint for '{title}' has been {dto.Status} by your instructor.",
            clickUrl: $"/courses/{courseId}/grades");

        return MapComplaintToDto(complaint, title);
    }

    // Helpers

    private async Task<(decimal TotalScore, decimal TotalMax)> GetAssignmentScoresAsync(int studentId, int courseId)
    {
        var assignments = await Assignments.GetAllAsync(new AssignmentSpec(courseId, byCourse: true), asNoTracking: true);
        var assignmentIds = assignments.Select(a => a.AssignmentId).ToHashSet();

        var submissions = (await StudentAssignments.GetAllAsync(new StudentAssignmentSpec(studentId, byStudent: true, dummy: true), asNoTracking: true))
            .Where(sa => assignmentIds.Contains(sa.AssignmentId) && sa.Grade.HasValue)
            .ToList();

        var totalScore = submissions.Sum(sa => sa.Grade!.Value);
        var totalMax = submissions.Sum(sa => assignments.FirstOrDefault(a => a.AssignmentId == sa.AssignmentId)?.MaxGrade ?? 0);

        return (totalScore, totalMax);
    }

    private async Task<(decimal TotalScore, decimal TotalMax)> GetQuizScoresAsync(int studentId, int courseId)
    {
        var quizzes = await Quizzes.GetAllAsync(new QuizSpec(courseId, byCourse: true), asNoTracking: true);
        var quizIds = quizzes.Select(q => q.QuizId).ToHashSet();

        var submissions = (await StudentQuizzes.GetAllAsync(new StudentQuizSpec(studentId, true, true), asNoTracking: true))
            .Where(sq => quizIds.Contains(sq.QuizId) && sq.Score.HasValue)
            .ToList();

        var totalScore = submissions.Sum(sq => sq.Score!.Value);
        var totalMax = submissions.Sum(sq => quizzes.FirstOrDefault(q => q.QuizId == sq.QuizId)?.MaxGrade ?? 0);

        return (totalScore, totalMax);
    }

    // In-memory variants of the per-course scoring helpers above, operating on
    // data that was batch-loaded once for all of the student's courses. These
    // replace the N+1 round-trips the transcript loop used to perform.
    // Weight resolution helpers

    private static (decimal AssignmentWeight, decimal QuizWeight, decimal MidtermWeight, decimal FinalWeight) ResolveGradeWeights(
        CourseWorkWeight? courseWorkWeight,
        Grade? midterm, Grade? final,
        BylawSettings? bylawSettings,
        decimal assignTotalMax, decimal quizTotalMax)
    {
        if (courseWorkWeight is not null)
        {
            var finalWeight = bylawSettings?.FinalExamGrade ?? final?.Weight ?? 0;
            return (courseWorkWeight.AssignmentWeight, courseWorkWeight.QuizWeight, courseWorkWeight.MidtermWeight, finalWeight);
        }

        var midtermWeight = midterm?.Weight ?? 0;
        var finalWeightLegacy = final?.Weight ?? 0;
        var assignQuizWeight = 100m - midtermWeight - finalWeightLegacy;
        if (assignQuizWeight < 0) assignQuizWeight = 0;

        var totalMax = assignTotalMax + quizTotalMax;
        var assignmentWeight = totalMax > 0 ? assignQuizWeight * assignTotalMax / totalMax : assignQuizWeight / 2;
        var quizWeight = totalMax > 0 ? assignQuizWeight * quizTotalMax / totalMax : assignQuizWeight / 2;

        return (assignmentWeight, quizWeight, midtermWeight, finalWeightLegacy);
    }

    private static (decimal AssignmentContrib, decimal QuizContrib, decimal MidtermContrib, decimal FinalContrib) CalculateContributions(
        decimal assignTotalScore, decimal assignTotalMax,
        decimal quizTotalScore, decimal quizTotalMax,
        decimal assignmentWeight, decimal quizWeight, decimal midtermWeight, decimal finalWeight,
        Grade? midterm, Grade? final)
    {
        var assignmentPct = assignTotalMax > 0 ? assignTotalScore / assignTotalMax * 100 : 0;
        var assignmentContrib = assignmentPct * assignmentWeight / 100;

        var quizPct = quizTotalMax > 0 ? quizTotalScore / quizTotalMax * 100 : 0;
        var quizContrib = quizPct * quizWeight / 100;

        var midtermPct = midterm is not null && midterm.MaxScore > 0 ? midterm.Score / midterm.MaxScore * 100 : 0;
        var midtermContrib = midtermPct * midtermWeight / 100;

        var finalPct = final is not null && final.MaxScore > 0 ? final.Score / final.MaxScore * 100 : 0;
        var finalContrib = finalPct * finalWeight / 100;

        return (assignmentContrib, quizContrib, midtermContrib, finalContrib);
    }

    private static (decimal TotalScore, decimal TotalMax) ComputeAssignmentScores(
        int courseId,
        Dictionary<int, Dictionary<int, decimal>> assignmentsMaxByCourse,
        IReadOnlyList<StudentAssignment> studentAssignments)
    {
        if (!assignmentsMaxByCourse.TryGetValue(courseId, out var maxByAssignment))
            return (0, 0);

        var assignmentIds = maxByAssignment.Keys;
        decimal totalScore = 0;
        decimal totalMax = 0;

        foreach (var sa in studentAssignments)
        {
            if (!assignmentIds.Contains(sa.AssignmentId) || !sa.Grade.HasValue)
                continue;

            totalScore += sa.Grade.Value;
            totalMax += maxByAssignment[sa.AssignmentId];
        }

        return (totalScore, totalMax);
    }

    private static (decimal TotalScore, decimal TotalMax) ComputeQuizScores(
        int courseId,
        Dictionary<int, Dictionary<int, decimal>> quizzesMaxByCourse,
        IReadOnlyList<StudentQuiz> studentQuizzes)
    {
        if (!quizzesMaxByCourse.TryGetValue(courseId, out var maxByQuiz))
            return (0, 0);

        var quizIds = maxByQuiz.Keys;
        decimal totalScore = 0;
        decimal totalMax = 0;

        foreach (var sq in studentQuizzes)
        {
            if (!quizIds.Contains(sq.QuizId) || !sq.Score.HasValue)
                continue;

            totalScore += sq.Score.Value;
            totalMax += maxByQuiz[sq.QuizId];
        }

        return (totalScore, totalMax);
    }

    private static (decimal TotalScore, decimal TotalMax) ComputeAssignmentGrade(
        List<StudentAssignment> gradedAssignments, IEnumerable<Assignment> assignments)
    {
        var totalScore = gradedAssignments.Sum(sa => sa.Grade!.Value);
        var totalMax = gradedAssignments.Sum(sa => assignments.FirstOrDefault(a => a.AssignmentId == sa.AssignmentId)?.MaxGrade ?? 0);
        return (totalScore, totalMax);
    }

    private static (decimal TotalScore, decimal TotalMax) ComputeQuizGrade(
        List<StudentQuiz> gradedQuizzes, IEnumerable<Quiz> quizzes)
    {
        var totalScore = gradedQuizzes.Sum(sq => sq.Score!.Value);
        var totalMax = gradedQuizzes.Sum(sq => quizzes.FirstOrDefault(q => q.QuizId == sq.QuizId)?.MaxGrade ?? 0);
        return (totalScore, totalMax);
    }

    private static (decimal TotalScore, decimal TotalMax) ComputeAssignmentScores(
        IEnumerable<StudentAssignment> submissions, IEnumerable<Assignment> assignments)
    {
        var graded = submissions.Where(sa => sa.Grade.HasValue).ToList();
        var assignDict = assignments.ToDictionary(a => a.AssignmentId);
        var totalScore = graded.Sum(sa => sa.Grade!.Value);
        var totalMax = graded.Sum(sa => assignDict.GetValueOrDefault(sa.AssignmentId)?.MaxGrade ?? 0);
        return (totalScore, totalMax);
    }

    private static (decimal TotalScore, decimal TotalMax) ComputeQuizScores(
        IEnumerable<StudentQuiz> submissions, IEnumerable<Quiz> quizzes)
    {
        var graded = submissions.Where(sq => sq.Score.HasValue).ToList();
        var quizDict = quizzes.ToDictionary(q => q.QuizId);
        var totalScore = graded.Sum(sq => sq.Score!.Value);
        var totalMax = graded.Sum(sq => quizDict.GetValueOrDefault(sq.QuizId)?.MaxGrade ?? 0);
        return (totalScore, totalMax);
    }

    private static CourseGradeDto BuildCourseGradeDto(decimal overallPercent, string letter, decimal gpa,
        List<AssessmentBreakdownDto> breakdown, List<GradeHistoryItemDto> history) => new()
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

    private async Task<Dictionary<int, decimal>> ComputeCourseworkPercentagesAsync(
        int studentId, List<int> courseIds, IEnumerable<Grade> allGrades)
    {
        var assignments = await Assignments.GetAllAsync(new AssignmentSpec(courseIds), asNoTracking: true);
        var assignmentByCourse = assignments.GroupBy(a => a.CourseId).ToDictionary(g => g.Key, g => g.ToList());
        var quizzes = await Quizzes.GetAllAsync(new QuizSpec(courseIds), asNoTracking: true);
        var quizByCourse = quizzes.GroupBy(q => q.CourseId).ToDictionary(g => g.Key, g => g.ToList());
        var submissions = (await StudentAssignments.GetAllAsync(
            new StudentAssignmentSpec(studentId, "transcript"), asNoTracking: true)).ToList();
        var quizSubmissions = (await StudentQuizzes.GetAllAsync(
            new StudentQuizSpec(studentId, "transcript"), asNoTracking: true)).ToList();
        var weights = (await CourseWorkWeights.GetAllAsync()).ToDictionary(w => w.CourseId);
        var midtermsByCourse = allGrades
            .Where(g => g.GradeType == GradeType.Midterm && g.Status == "Graded")
            .GroupBy(g => g.CourseId)
            .ToDictionary(g => g.Key, g => g.First());

        var result = new Dictionary<int, decimal>();

        foreach (var courseId in courseIds)
        {
            var courseAssignments = assignmentByCourse.GetValueOrDefault(courseId);
            var courseQuizzes = quizByCourse.GetValueOrDefault(courseId);
            var courseSubmissions = courseAssignments is not null
                ? submissions.Where(s => courseAssignments.Any(a => a.AssignmentId == s.AssignmentId)).ToList()
                : new List<StudentAssignment>();
            var courseQuizSubmissions = courseQuizzes is not null
                ? quizSubmissions.Where(qs => courseQuizzes.Any(q => q.QuizId == qs.QuizId)).ToList()
                : new List<StudentQuiz>();
            var midterm = midtermsByCourse.GetValueOrDefault(courseId);
            var weight = weights.GetValueOrDefault(courseId);

            var assignTotal = courseAssignments is not null
                ? ComputeAssignmentScores(courseSubmissions, courseAssignments)
                : (TotalScore: 0m, TotalMax: 0m);
            var quizTotal = courseQuizzes is not null
                ? ComputeQuizScores(courseQuizSubmissions, courseQuizzes)
                : (TotalScore: 0m, TotalMax: 0m);

            decimal courseworkPct;
            if (weight is not null)
            {
                var (aContrib, qContrib, mContrib, _) = CalculateContributions(
                    assignTotal.TotalScore, assignTotal.TotalMax,
                    quizTotal.TotalScore, quizTotal.TotalMax,
                    weight.AssignmentWeight, weight.QuizWeight, weight.MidtermWeight, 0,
                    midterm, null);
                var cwContrib = aContrib + qContrib + mContrib;
                var cwWeight = weight.AssignmentWeight + weight.QuizWeight + weight.MidtermWeight;
                courseworkPct = cwWeight > 0 ? Math.Round(cwContrib / cwWeight * 100, 0) : 100;
            }
            else
            {
                var midtermWeight = midterm?.Weight ?? 0;
                var totalWeight = 100m;
                var assignQuizWeight = totalWeight - midtermWeight;
                var assignQuizContrib = assignTotal.TotalMax > 0 || quizTotal.TotalMax > 0
                    ? ((assignTotal.TotalMax > 0 ? assignTotal.TotalScore / assignTotal.TotalMax * assignQuizWeight : 0)
                     + (quizTotal.TotalMax > 0 ? quizTotal.TotalScore / quizTotal.TotalMax * assignQuizWeight : 0)) / 2
                    : 0;
                var midContrib = midterm?.MaxScore > 0
                    ? midterm.Score / midterm.MaxScore * midtermWeight
                    : 0;
                var cwContrib = assignQuizContrib + midContrib;
                var cwWeight = totalWeight;
                courseworkPct = Math.Round(cwContrib / cwWeight * 100, 0);
            }

            result[courseId] = courseworkPct;
        }

        return result;
    }

    private static (decimal TotalScore, decimal TotalMax) ComputeAssignmentScores(
        List<StudentAssignment> submissions, List<Assignment> assignments)
    {
        var graded = submissions.Where(sa => sa.Grade.HasValue).ToList();
        var assignDict = assignments.ToDictionary(a => a.AssignmentId);
        var totalScore = graded.Sum(sa => sa.Grade!.Value);
        var totalMax = graded.Sum(sa => assignDict.GetValueOrDefault(sa.AssignmentId)?.MaxGrade ?? 0);
        return (totalScore, totalMax);
    }

    private static decimal ApplyBylawGradeRules(
        BylawSettings? settings, List<GradeScaleItem>? gradeScales,
        HashSet<int> failedCourseIds, int courseId,
        decimal assignQuizWeight, decimal midtermWeight, decimal finalWeight,
        decimal assignQuizContrib, decimal midtermContrib, decimal finalContrib,
        decimal overallPercent,
        out bool isForcedFailing)
    {
        isForcedFailing = false;
        if (settings is null)
            return overallPercent;

        var courseworkWeight = assignQuizWeight + midtermWeight;

        if (settings.MinPassingCourseworkGrade.HasValue && courseworkWeight > 0)
        {
            var courseworkPct = Math.Round((assignQuizContrib + midtermContrib) / courseworkWeight * 100, 0);
            if (courseworkPct < settings.MinPassingCourseworkGrade.Value)
            {
                isForcedFailing = true;
                return overallPercent;
            }
        }

        if (settings.MinPassingFinalExamGrade.HasValue && finalWeight > 0)
        {
            var finalPct = Math.Round(finalContrib / finalWeight * 100, 0);
            if (finalPct < settings.MinPassingFinalExamGrade.Value)
            {
                isForcedFailing = true;
                return overallPercent;
            }
        }

        if (!string.IsNullOrWhiteSpace(settings.MaxGradeOnRetake) && failedCourseIds.Contains(courseId))
        {
            var scale = gradeScales?.FirstOrDefault(s => s.GradeLetter == settings.MaxGradeOnRetake);
            if (scale is not null && overallPercent > scale.MinPercentage)
                return scale.MinPercentage;
        }

        return overallPercent;
    }

    private static string ResolveGradeScale(List<GradeScaleItem>? scales, decimal percent)
    {
        if (scales?.Count > 0)
        {
            var scale = scales
                .OrderByDescending(s => s.MinPercentage)
                .FirstOrDefault(s => percent >= s.MinPercentage);

            if (scale is not null)
                return scale.GradeLetter;
        }

        return "-";
    }

    private async Task<(decimal OverallPercent, bool IsForcedFailing)> ApplyBylawGradeRulesAsync(int studentId, int courseId,
        decimal assignQuizWeight, decimal midtermWeight, decimal finalWeight,
        decimal assignQuizContrib, decimal midtermContrib, decimal finalContrib,
        decimal overallPercent)
    {
        var spec = new StudentSpec(new CourseQueryParams { StudentId = studentId });
        var student = await Students.GetByIdAsync(spec);
        var settings = student?.Bylaw?.Settings;
        if (settings is null) return (overallPercent, false);

        var courseworkWeight = assignQuizWeight + midtermWeight;

        if (settings.MinPassingCourseworkGrade.HasValue && courseworkWeight > 0)
        {
            var courseworkPct = Math.Round((assignQuizContrib + midtermContrib) / courseworkWeight * 100, 0);
            if (courseworkPct < settings.MinPassingCourseworkGrade.Value)
                return (overallPercent, true);
        }

        if (settings.MinPassingFinalExamGrade.HasValue && finalWeight > 0)
        {
            var finalPct = Math.Round(finalContrib / finalWeight * 100, 0);
            if (finalPct < settings.MinPassingFinalExamGrade.Value)
                return (overallPercent, true);
        }

        if (!string.IsNullOrWhiteSpace(settings.MaxGradeOnRetake))
        {
            var hasFailedBefore = await StudentCourses.AnyAsync(sc =>
                sc.StudentId == studentId && sc.CourseId == courseId && sc.Status == StudentCourseStatus.Failed);

            if (!hasFailedBefore)
            {
                var course = await Courses.GetByIdAsync(courseId);
                var courseCode = course?.CourseCode;
                if (!string.IsNullOrWhiteSpace(courseCode))
                {
                    var passingThreshold = settings.MinPassingFinalExamGrade ?? 50m;
                    hasFailedBefore = await Grades.AnyAsync(g =>
                        g.StudentId == studentId &&
                        g.Course!.CourseCode == courseCode &&
                        g.GradeType == GradeType.Final &&
                        g.Score < passingThreshold);
                }
            }

            if (hasFailedBefore)
            {
                var scale = student?.Bylaw?.GradeScales?
                    .FirstOrDefault(s => s.GradeLetter == settings.MaxGradeOnRetake);
                if (scale is not null && overallPercent > scale.MinPercentage)
                    return (scale.MinPercentage, false);
            }
        }

        return (overallPercent, false);
    }

    private async Task<(decimal MidtermW, decimal FinalW, decimal QuizW, decimal AssignW)> ResolveCourseWeightsAsync(int courseId, Grade? midterm, Grade? final)
    {
        var existing = (await CourseWorkWeights.GetAllAsync()).FirstOrDefault(w => w.CourseId == courseId);
        if (existing is not null)
        {
            var finalWeight = 100m - existing.QuizWeight - existing.AssignmentWeight - existing.MidtermWeight;
            if (finalWeight < 0) finalWeight = 0;
            return (existing.MidtermWeight, finalWeight, existing.QuizWeight, existing.AssignmentWeight);
        }
        var midtermW = midterm?.Weight ?? 0;
        var finalW = final?.Weight ?? 0;
        var assignQuizW = 100m - midtermW - finalW;
        if (assignQuizW < 0) assignQuizW = 0;
        return (midtermW, finalW, assignQuizW / 2, assignQuizW / 2);
    }

    private static (decimal AssignQuizContrib, decimal MidtermContrib, decimal FinalContrib) CalculateWeightedContributions(
        decimal assignTotalScore, decimal assignTotalMax,
        decimal quizTotalScore, decimal quizTotalMax,
        Grade? midterm, Grade? final)
    {
        var midtermWeight = midterm?.Weight ?? 0;
        var finalWeight = final?.Weight ?? 0;
        var assignQuizWeight = 100 - midtermWeight - finalWeight;
        if (assignQuizWeight < 0) assignQuizWeight = 0;

        var totalMax = assignTotalMax + quizTotalMax;
        var assignmentWeight = totalMax > 0 ? assignQuizWeight * assignTotalMax / totalMax : assignQuizWeight / 2;
        var quizWeight = totalMax > 0 ? assignQuizWeight * quizTotalMax / totalMax : assignQuizWeight / 2;

        var assignPct = assignTotalMax > 0 ? assignTotalScore / assignTotalMax * 100 : 0;
        var assignContrib = assignPct * assignmentWeight / 100;

        var quizPct = quizTotalMax > 0 ? quizTotalScore / quizTotalMax * 100 : 0;
        var quizContrib = quizPct * quizWeight / 100;

        var midtermPct = midterm is not null && midterm.MaxScore > 0 ? midterm.Score / midterm.MaxScore * 100 : 0;
        var midtermContrib = midtermPct * midtermWeight / 100;

        var finalPct = final is not null && final.MaxScore > 0 ? final.Score / final.MaxScore * 100 : 0;
        var finalContrib = finalPct * finalWeight / 100;

        return (assignContrib + quizContrib, midtermContrib, finalContrib);
    }

    private async Task<(string Letter, decimal Gpa)> ResolveGradeScaleAsync(int studentId, decimal percent)
    {
        var spec = new StudentSpec(new CourseQueryParams { StudentId = studentId });
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

    private async Task<string> ResolveComplaintTitleAsync(int gradeId, string complaintType)
    {
        var type = complaintType.ToLowerInvariant();
        return type switch
        {
            "assignment" => await ResolveAssignmentTitle(gradeId),
            "quiz" => await ResolveQuizTitle(gradeId),
            _ => await ResolveGradeTitle(gradeId)
        };
    }

    private async Task<Dictionary<int, string>> BatchResolveTitlesAsync(IEnumerable<GradeComplaint> complaints)
    {
        var result = new Dictionary<int, string>();
        var assignPks = new List<int>();
        var quizPks = new List<int>();
        var gradePks = new List<int>();

        foreach (var c in complaints)
        {
            switch (c.ComplaintType.ToLowerInvariant())
            {
                case "assignment": assignPks.Add(c.GradeId); break;
                case "quiz": quizPks.Add(c.GradeId); break;
                default: gradePks.Add(c.GradeId); break;
            }
        }

        if (assignPks.Count > 0)
        {
            var submissions = await StudentAssignments.GetAllAsync(new StudentAssignmentSpec(assignPks, "batch"), asNoTracking: true);
            var assignIds = submissions.Select(s => s.AssignmentId).ToHashSet();
            var assignments = await Assignments.GetAllAsync(new AssignmentSpec(assignIds.ToList(), byIds: true), asNoTracking: true);
            var titleById = assignments.ToDictionary(a => a.AssignmentId, a => a.Title);
            foreach (var s in submissions)
            {
                result[s.StudentAssignmentId] = titleById.GetValueOrDefault(s.AssignmentId, "");
            }
        }

        if (quizPks.Count > 0)
        {
            var quizzes = await Quizzes.GetAllAsync(new QuizSpec(quizPks, byIds: true), asNoTracking: true);
            foreach (var q in quizzes)
            {
                result[q.QuizId] = q.Title;
            }
        }

        if (gradePks.Count > 0)
        {
            var grades = await Grades.GetAllAsync(new GradeSpec(gradePks, true), asNoTracking: true);
            foreach (var g in grades)
            {
                result[g.GradeId] = g.Title;
            }
        }

        return result;
    }

    private async Task<string> ValidateAssignmentComplaint(int studentId, int gradeId)
    {
        var submission = await StudentAssignments.GetByIdAsync(gradeId);
        if (submission is null || submission.StudentId != studentId)
            throw new GradeNotFoundException(gradeId);
        if (!submission.Grade.HasValue)
            throw new InvalidOperationException("Cannot complain about an ungraded submission.");
        var assignment = await Assignments.GetByIdAsync(submission.AssignmentId);
        return assignment?.Title ?? string.Empty;
    }

    private async Task<string> ValidateQuizComplaint(int studentId, int quizId)
    {
        var spec = new StudentQuizSpec(studentId, quizId);
        var submission = await StudentQuizzes.GetByIdAsync(spec);
        if (submission is null)
            throw new GradeNotFoundException(quizId);
        if (!submission.Score.HasValue)
            throw new InvalidOperationException("Cannot complain about an ungraded quiz.");
        var quiz = await Quizzes.GetByIdAsync(quizId);
        return quiz?.Title ?? string.Empty;
    }

    private async Task<string> ValidateGradeComplaint(int studentId, int gradeId, string complaintType)
    {
        var grade = await Grades.GetByIdAsync(gradeId);
        if (grade is null || grade.StudentId != studentId)
            throw new GradeNotFoundException(gradeId);
        return grade.Title;
    }

    private async Task<string> ResolveAssignmentTitle(int gradeId)
    {
        var submission = await StudentAssignments.GetByIdAsync(gradeId);
        if (submission is null) return string.Empty;
        var assignment = await Assignments.GetByIdAsync(submission.AssignmentId);
        return assignment?.Title ?? string.Empty;
    }

    private async Task<string> ResolveQuizTitle(int quizId)
    {
        var quiz = await Quizzes.GetByIdAsync(quizId);
        return quiz?.Title ?? string.Empty;
    }

    private async Task<string> ResolveGradeTitle(int gradeId)
    {
        var grade = await Grades.GetByIdAsync(gradeId);
        return grade?.Title ?? string.Empty;
    }

    private static GradeComplaintResponseDto MapComplaintToDto(GradeComplaint c, string gradeTitle) => new()
    {
        ComplaintId = c.ComplaintId,
        GradeId = c.GradeId,
        Title = gradeTitle,
        ComplaintType = c.ComplaintType,
        Details = c.Details,
        Status = c.Status.ToString().ToLower(),
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

    private static int? ExtractLevelFromCourseCode(string? courseCode)
    {
        if (string.IsNullOrEmpty(courseCode))
            return null;

        var digits = new string(courseCode.Where(char.IsDigit).ToArray());
        if (digits.Length == 0)
            return null;

        var firstDigit = digits[0] - '0';
        return firstDigit >= 1 && firstDigit <= 5 ? firstDigit : null;
    }

    public async Task<CourseWorkWeightDto> GetCourseWorkWeightAsync(int courseId, int instructorId)
    {
        var course = await Courses.GetByIdAsync(courseId);
        if (course is null)
            throw new CourseNotFoundException(courseId);

        var teaches = await Classes.AnyAsync(c => c.CourseId == courseId && c.InstructorId == instructorId);
        if (!teaches)
            throw new InvalidOperationException("Not authorized.");

        var weight = await CourseWorkWeights.GetByIdAsync(courseId);
        if (weight is null)
            return new CourseWorkWeightDto { CourseId = courseId, QuizWeight = 0, AssignmentWeight = 0, MidtermWeight = 0 };

        return new CourseWorkWeightDto
        {
            CourseId = weight.CourseId,
            QuizWeight = weight.QuizWeight,
            AssignmentWeight = weight.AssignmentWeight,
            MidtermWeight = weight.MidtermWeight
        };
    }

    public async Task SetCourseWorkWeightAsync(int courseId, int instructorId, CourseWorkWeightDto dto)
    {
        var course = await Courses.GetByIdAsync(courseId);
        if (course is null)
            throw new CourseNotFoundException(courseId);

        if (course.Status != CourseStatus.Active)
            throw new InvalidOperationException("This course is finalized and read-only.");

        var isProfessor = await Classes.AnyAsync(c =>
            c.CourseId == courseId && c.InstructorId == instructorId && c.ClassType == ClassType.Lecture);
        if (!isProfessor)
            throw new InvalidOperationException("Not authorized. Only the course professor can manage coursework weights.");

        var totalCoursework = dto.QuizWeight + dto.AssignmentWeight + dto.MidtermWeight;

        decimal? bylawCourseWork = null;

        var enrolledStudentCourses = (await StudentCourses.GetAllAsync(new StudentCourseIdsSpec(courseId, true), asNoTracking: true)).ToList();
        if (enrolledStudentCourses.Count > 0)
        {
            var studentSpec = new StudentSpec(new CourseQueryParams { StudentId = enrolledStudentCourses[0].StudentId });
            var anyStudent = await Students.GetByIdAsync(studentSpec);
            bylawCourseWork = anyStudent?.Bylaw?.Settings?.CourseWorkGrade;
        }
        else
        {
            var bylawCourse = (await _unitOfWork.GetRepository<BylawCourse, int>().GetAllAsync())
                .FirstOrDefault(bc => bc.CourseId == courseId);
            if (bylawCourse is not null)
            {
                var bylawSpec = new BylawSpec(bylawCourse.BylawId);
                var bylaw = await _unitOfWork.GetRepository<Bylaw, int>().GetByIdAsync(bylawSpec);
                bylawCourseWork = bylaw?.Settings?.CourseWorkGrade;
            }
        }

        if (bylawCourseWork.HasValue && totalCoursework != bylawCourseWork.Value)
            throw new InvalidOperationException(
                $"Coursework weights ({totalCoursework}) must equal the bylaw's course work grade ({bylawCourseWork.Value}).");

        var existing = await CourseWorkWeights.GetByIdAsync(courseId);
        if (existing is not null)
        {
            existing.QuizWeight = dto.QuizWeight;
            existing.AssignmentWeight = dto.AssignmentWeight;
            existing.MidtermWeight = dto.MidtermWeight;
            CourseWorkWeights.Update(existing);
        }
        else
        {
            CourseWorkWeights.Add(new CourseWorkWeight
            {
                CourseId = courseId,
                QuizWeight = dto.QuizWeight,
                AssignmentWeight = dto.AssignmentWeight,
                MidtermWeight = dto.MidtermWeight
            });
        }
        await _unitOfWork.SaveChangesAsync();
    }
}
