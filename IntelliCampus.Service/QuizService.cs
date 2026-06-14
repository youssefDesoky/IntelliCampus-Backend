using System.Text.Json;
using IntelliCampus.Domain.Entities;
using IntelliCampus.Domain.Entities.Enums;
using IntelliCampus.Domain.Interfaces;
using IntelliCampus.Service_Abstraction;
using IntelliCampus.shared.Dtos.Quiz;
using IntelliCampus.Service.Specifications;

namespace IntelliCampus.Service;

public class QuizService : IQuizService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly INotificationService _notificationService;

    public QuizService(IUnitOfWork unitOfWork, INotificationService notificationService)
    {
        _unitOfWork = unitOfWork;
        _notificationService = notificationService;
    }

    private IGenericRepository<Quiz, int> Quizzes
        => _unitOfWork.GetRepository<Quiz, int>();

    private IGenericRepository<StudentQuiz, int> StudentQuizzes
        => _unitOfWork.GetRepository<StudentQuiz, int>();

    private IGenericRepository<Class, int> Classes
        => _unitOfWork.GetRepository<Class, int>();

    private IGenericRepository<Course, int> Courses
        => _unitOfWork.GetRepository<Course, int>();

    private IGenericRepository<Question, int> QuestionsRepo
        => _unitOfWork.GetRepository<Question, int>();

    public async Task<QuizHistoryItemDto?> GetByIdAsync(int quizId, int studentId)
    {
        var spec = new QuizSpec(quizId);
        var quiz = await Quizzes.GetByIdAsync(spec);
        if (quiz is null) return null;

        var submission = await StudentQuizzes.GetByIdAsync(new StudentQuizSpec(studentId, quizId));
        var hasSubmission = submission is not null;

        return new QuizHistoryItemDto
        {
            Id = quiz.QuizId.ToString(),
            Title = quiz.Title,
            Score = submission?.Score,
            MaxScore = quiz.MaxGrade,
            DurationMinutes = quiz.DurationMinutes,
            StartDate = quiz.StartDate,
            DueDate = quiz.DueDate,
            Status = hasSubmission ? "Completed" : quiz.DueDate < DateTime.UtcNow ? "Overdue" :
                     quiz.StartDate > DateTime.UtcNow ? "Upcoming" : "Active"
        };
    }

    public async Task<QuizHistoryItemDto?> GetByIdInCourseAsync(int quizId, int studentId, string courseId)
    {
        if (!int.TryParse(courseId, out var parsedCourseId))
            return null;

        var course = await Courses.GetByIdAsync(parsedCourseId);
        if (course is null) return null;

        var spec = new QuizSpec(quizId);
        var quiz = await Quizzes.GetByIdAsync(spec);
        if (quiz is null) return null;

        if (quiz.CourseId != parsedCourseId)
            return null;

        var submission = await StudentQuizzes.GetByIdAsync(new StudentQuizSpec(studentId, quizId));
        var hasSubmission = submission is not null;

        return new QuizHistoryItemDto
        {
            Id = quiz.QuizId.ToString(),
            Title = quiz.Title,
            Score = submission?.Score,
            MaxScore = quiz.MaxGrade,
            DurationMinutes = quiz.DurationMinutes,
            StartDate = quiz.StartDate,
            DueDate = quiz.DueDate,
            Status = hasSubmission ? "Completed" : quiz.DueDate < DateTime.UtcNow ? "Overdue" :
                     quiz.StartDate > DateTime.UtcNow ? "Upcoming" : "Active"
        };
    }

    public async Task<IEnumerable<QuizDto>> GetByCourseIdAsync(int courseId)
    {
        var spec = new QuizSpec(courseId, byCourse: true);
        var quizzes = await Quizzes.GetAllAsync(spec);
        return quizzes.Select(MapToDto);
    }

    public async Task<QuizDto> CreateAsync(int instructorId, CreateQuizDto dto)
    {
        var course = await Courses.GetByIdAsync(dto.CourseId);
        if (course is null)
            throw new InvalidOperationException("Course not found.");

        var teachesCourse = await Classes.AnyAsync(c => c.CourseId == dto.CourseId && c.InstructorId == instructorId);
        if (!teachesCourse)
            throw new InvalidOperationException("Not authorized to create quizzes for this course.");

        var quiz = new Quiz
        {
            Title = dto.Title,
            Description = dto.Description,
            StartDate = dto.StartDate,
            DueDate = dto.DueDate,
            DurationMinutes = dto.DurationMinutes,
            MaxGrade = dto.MaxGrade,
            TotalMarks = (int)dto.MaxGrade,
            CourseId = dto.CourseId
        };

        Quizzes.Add(quiz);
        await _unitOfWork.SaveChangesAsync();

        var spec = new QuizSpec(quiz.QuizId);
        var result = await Quizzes.GetByIdAsync(spec);
        return MapToDto(result!);
    }

    public async Task<bool> DeleteAsync(int quizId, int instructorId)
    {
        var quiz = await Quizzes.GetByIdAsync(quizId);
        if (quiz is null) return false;

        var teachesCourse = await Classes.AnyAsync(c => c.CourseId == quiz.CourseId && c.InstructorId == instructorId);
        if (!teachesCourse)
            throw new InvalidOperationException("Not authorized.");

        Quizzes.Delete(quiz);
        await _unitOfWork.SaveChangesAsync();
        return true;
    }

    public async Task<QuizDto> CreateInCourseAsync(int instructorId, string courseId, CreateQuizDto dto)
    {
        if (!int.TryParse(courseId, out var parsedCourseId))
            throw new InvalidOperationException("Course not found.");

        var course = await Courses.GetByIdAsync(parsedCourseId);
        if (course is null)
            throw new InvalidOperationException("Course not found.");

        var teachesCourse = await Classes.AnyAsync(c => c.CourseId == parsedCourseId && c.InstructorId == instructorId);
        if (!teachesCourse)
            throw new InvalidOperationException("Not authorized to create quizzes for this course.");

        var quiz = new Quiz
        {
            Title = dto.Title,
            Description = dto.Description,
            StartDate = dto.StartDate,
            DueDate = dto.DueDate,
            DurationMinutes = dto.DurationMinutes,
            MaxGrade = dto.MaxGrade,
            TotalMarks = (int)dto.MaxGrade,
            CourseId = parsedCourseId
        };

        Quizzes.Add(quiz);
        await _unitOfWork.SaveChangesAsync();

        var spec = new QuizSpec(quiz.QuizId);
        var result = await Quizzes.GetByIdAsync(spec);
        return MapToDto(result!);
    }

    public async Task<bool> DeleteInCourseAsync(int quizId, int instructorId, string courseId)
    {
        if (!int.TryParse(courseId, out var parsedCourseId))
            return false;

        var course = await Courses.GetByIdAsync(parsedCourseId);
        if (course is null) return false;

        var spec = new QuizSpec(quizId);
        var quiz = await Quizzes.GetByIdAsync(spec);
        if (quiz is null) return false;

        if (quiz.CourseId != parsedCourseId)
            return false;

        var teachesCourse = await Classes.AnyAsync(c => c.CourseId == quiz.CourseId && c.InstructorId == instructorId);
        if (!teachesCourse)
            throw new InvalidOperationException("Not authorized.");

        Quizzes.Delete(quiz);
        await _unitOfWork.SaveChangesAsync();
        return true;
    }

    public async Task AddQuestionsAsync(int quizId, int instructorId, string courseId, List<CreateQuestionDto> questions)
    {
        if (!int.TryParse(courseId, out var parsedCourseId))
            throw new InvalidOperationException("Course not found.");

        var course = await Courses.GetByIdAsync(parsedCourseId);
        if (course is null)
            throw new InvalidOperationException("Course not found.");

        var spec = new QuizSpec(quizId);
        var quiz = await Quizzes.GetByIdAsync(spec);
        if (quiz is null)
            throw new InvalidOperationException("Quiz not found.");

        if (quiz.CourseId != parsedCourseId)
            throw new InvalidOperationException("Quiz does not belong to this course.");

        var teachesCourse = await Classes.AnyAsync(c => c.CourseId == quiz.CourseId && c.InstructorId == instructorId);
        if (!teachesCourse)
            throw new InvalidOperationException("Not authorized.");

        foreach (var q in questions)
        {
            var question = new Question
            {
                QuizId = quizId,
                Type = q.Type,
                Prompt = q.Prompt,
                Options = q.Options is not null ? System.Text.Json.JsonSerializer.Serialize(q.Options) : null,
                Points = q.Points,
                CorrectAnswer = q.CorrectAnswer
            };
            QuestionsRepo.Add(question);
        }

        await _unitOfWork.SaveChangesAsync();
    }

    public async Task DeleteQuestionAsync(int questionId, int instructorId, string courseId)
    {
        if (!int.TryParse(courseId, out var parsedCourseId))
            throw new InvalidOperationException("Course not found.");

        var course = await Courses.GetByIdAsync(parsedCourseId);
        if (course is null)
            throw new InvalidOperationException("Course not found.");

        var question = await QuestionsRepo.GetByIdAsync(questionId);
        if (question is null)
            throw new InvalidOperationException("Question not found.");

        var spec = new QuizSpec(question.QuizId);
        var quiz = await Quizzes.GetByIdAsync(spec);
        if (quiz is null || quiz.CourseId != parsedCourseId)
            throw new InvalidOperationException("Question does not belong to this course.");

        var teachesCourse = await Classes.AnyAsync(c => c.CourseId == quiz.CourseId && c.InstructorId == instructorId);
        if (!teachesCourse)
            throw new InvalidOperationException("Not authorized.");

        QuestionsRepo.Delete(question);
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task<List<StudentSubmissionDto>> GetSubmissionsAsync(int quizId, int instructorId, string courseId)
    {
        if (!int.TryParse(courseId, out var parsedCourseId))
            return [];

        var course = await Courses.GetByIdAsync(parsedCourseId);
        if (course is null) return [];

        var spec = new QuizSpec(quizId);
        var quiz = await Quizzes.GetByIdAsync(spec);
        if (quiz is null || quiz.CourseId != parsedCourseId)
            return [];

        var teachesCourse = await Classes.AnyAsync(c => c.CourseId == quiz.CourseId && c.InstructorId == instructorId);
        if (!teachesCourse)
            return [];

        var allQuestions = (await QuestionsRepo.GetAllAsync(new QuestionsByQuizSpec(quizId))).ToList();
        var maxScore = allQuestions.Sum(q => q.Points);

        var subsSpec = new StudentQuizSpec(quizId, allResults: true);
        var submissions = await StudentQuizzes.GetAllAsync(subsSpec);

        return submissions.Select(sq => new StudentSubmissionDto
        {
            StudentId = sq.StudentId,
            StudentName = sq.Student?.FullName,
            Score = sq.Score,
            MaxScore = maxScore,
            SubmittedAt = sq.SubmittedAt.ToString("dd MM yy HH:mm"),
            Answers = sq.AnswersJson is not null ? JsonSerializer.Deserialize<Dictionary<string, object>>(sq.AnswersJson) : null,
            QuestionResults = sq.QuestionResultsJson is not null ? JsonSerializer.Deserialize<List<QuestionResultDto>>(sq.QuestionResultsJson) : null
        }).ToList();
    }

    public async Task GradeWrittenAsync(int quizId, int studentId, int instructorId, string courseId, GradeWrittenDto dto)
    {
        if (!int.TryParse(courseId, out var parsedCourseId))
            throw new InvalidOperationException("Course not found.");

        var course = await Courses.GetByIdAsync(parsedCourseId);
        if (course is null)
            throw new InvalidOperationException("Course not found.");

        var spec = new QuizSpec(quizId);
        var quiz = await Quizzes.GetByIdAsync(spec);
        if (quiz is null || quiz.CourseId != parsedCourseId)
            throw new InvalidOperationException("Quiz not found.");

        var teachesCourse = await Classes.AnyAsync(c => c.CourseId == quiz.CourseId && c.InstructorId == instructorId);
        if (!teachesCourse)
            throw new InvalidOperationException("Not authorized.");

        var existing = await StudentQuizzes.GetByIdAsync(new StudentQuizSpec(studentId, quizId));
        if (existing is null)
            throw new InvalidOperationException("Submission not found.");

        var allQuestions = (await QuestionsRepo.GetAllAsync(new QuestionsByQuizSpec(quizId))).ToList();
        var existingResults = existing.QuestionResultsJson is not null
            ? JsonSerializer.Deserialize<List<QuestionResultDto>>(existing.QuestionResultsJson)
            : null;

        decimal newTotal = 0;
        if (existingResults is not null)
        {
            foreach (var r in existingResults)
            {
                if (dto.QuestionScores.TryGetValue(r.QuestionId, out var manualScore))
                    r.EarnedPoints = manualScore;
                newTotal += r.EarnedPoints;
            }
        }

        existing.Score = newTotal;
        existing.QuestionResultsJson = JsonSerializer.Serialize(existingResults);
        StudentQuizzes.Update(existing);
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task<StudentQuizDto> SubmitAsync(int studentId, SubmitQuizDto dto)
    {
        // This is a placeholder since SubmitQuizDto is expecting answers
        // Originally:
        // var quiz = await Quizzes.GetByIdAsync(dto.QuizId); ...
        throw new NotImplementedException("Use SubmitPracticeQuizAsync for JSON payload compatibility or adjust DTO.");
    }

    public async Task<StudentQuizDto?> GetResultAsync(int studentId, int quizId)
    {
        var spec = new StudentQuizSpec(studentId, quizId);
        var result = await StudentQuizzes.GetByIdAsync(spec);
        return result is null ? null : MapResultToDto(result);
    }

    public async Task<IEnumerable<StudentQuizDto>> GetAllResultsAsync(int quizId, int instructorId)
    {
        var quiz = await Quizzes.GetByIdAsync(quizId);
        if (quiz is null)
            throw new InvalidOperationException("Quiz not found.");

        var teachesCourse = await Classes.AnyAsync(c => c.CourseId == quiz.CourseId && c.InstructorId == instructorId);
        if (!teachesCourse)
            throw new InvalidOperationException("Not authorized.");

        var spec = new StudentQuizSpec(quizId, allResults: true);
        var results = await StudentQuizzes.GetAllAsync(spec);
        return results.Select(MapResultToDto);
    }

    public async Task<IEnumerable<StudentQuizDto>> GetByStudentIdAsync(int studentId)
    {
        var spec = new StudentQuizSpec(studentId, byStudent: true, dummy: true);
        var results = await StudentQuizzes.GetAllAsync(spec);
        return results.Select(MapResultToDto);
    }

    private static QuizDto MapToDto(Quiz q) => new()
    {
        Id = q.QuizId,
        Title = q.Title,
        Description = q.Description,
        StartDate = q.StartDate,
        DueDate = q.DueDate,
        DurationMinutes = q.DurationMinutes,
        MaxScore = q.MaxGrade,
        CourseId = q.CourseId,
        CourseName = q.Course?.CourseName,
        Status = q.DueDate < DateTime.UtcNow ? "Completed" :
                 q.StartDate > DateTime.UtcNow ? "Upcoming" : "Active"
    };

    private static StudentQuizDto MapResultToDto(StudentQuiz sq) => new()
    {
        StudentId = sq.StudentId,
        StudentName = sq.Student?.FullName,
        QuizId = sq.QuizId,
        QuizTitle = sq.Quiz?.Title,
        Score = sq.Score,
        MaxGrade = sq.Quiz?.MaxGrade ?? 0,
        SubmittedAt = sq.SubmittedAt.ToString("dd MM yy HH:mm"),
        IsLate = sq.IsLate
    };

    public async Task<QuizSubmitResponseDto?> SubmitPracticeQuizAsync(int studentId, string courseId, SubmitQuizDto dto)
    {
        if (!int.TryParse(courseId, out var parsedCourseId))
            return null;

        var course = await Courses.GetByIdAsync(parsedCourseId);
        if (course is null)
            return null;

        var quiz = await Quizzes.GetByIdAsync(dto.QuizId);
        if (quiz is null)
            return null;

        var now = DateTime.UtcNow;
        if (now < quiz.StartDate || now > quiz.DueDate)
            return null;

        var existing = await StudentQuizzes.GetByIdAsync(new StudentQuizSpec(studentId, quiz.QuizId));
        var allQ = (await QuestionsRepo.GetAllAsync(new QuestionsByQuizSpec(quiz.QuizId))).ToList();
        var (qResults, bType, _) = GradeAnswers(allQ, dto.Answers);

        decimal? finalScore = null;

        var answersJson = JsonSerializer.Serialize(dto.Answers);
        var resultsJson = JsonSerializer.Serialize(qResults);

        if (existing is not null)
        {
            existing.Score = finalScore;
            existing.SubmittedAt = DateTime.UtcNow;
            existing.AnswersJson = answersJson;
            existing.QuestionResultsJson = resultsJson;
            StudentQuizzes.Update(existing);
            await _unitOfWork.SaveChangesAsync();

            var maxScore = allQ.Sum(q => q.Points);
            return new QuizSubmitResponseDto
            {
                CourseId = courseId,
                CourseName = course.CourseName,
                Score = finalScore,
                MaxScore = maxScore,
                Percentage = maxScore > 0 && finalScore.HasValue ? Math.Round(finalScore.Value / maxScore * 100, 0) : 0,
                AnsweredCount = dto.Answers.Count,
                ByType = bType.ToDictionary(kv => kv.Key, kv => new QuizTypeStatsDto { Answered = kv.Value.Answered, Total = kv.Value.Total, Score = kv.Value.Score }),
                QuestionResults = qResults,
                Answers = dto.Answers,
                SubmittedAt = existing.SubmittedAt.ToString("dd MM yy HH:mm")
            };
        }

        var studentQuiz = new StudentQuiz
        {
            StudentId = studentId,
            QuizId = quiz.QuizId,
            Score = finalScore,
            SubmittedAt = DateTime.UtcNow,
            IsLate = now > quiz.DueDate,
            AnswersJson = answersJson,
            QuestionResultsJson = resultsJson
        };
        StudentQuizzes.Add(studentQuiz);
        await _unitOfWork.SaveChangesAsync();

        await _notificationService.SendAsync(
            studentId,
            NotificationType.QuizSubmitted,
            $"Your quiz '{quiz.Title}' was submitted successfully.");

        var maxS = allQ.Sum(q => q.Points);
        return new QuizSubmitResponseDto
        {
            CourseId = courseId,
            CourseName = course.CourseName,
            Score = finalScore,
            MaxScore = maxS,
            Percentage = maxS > 0 && finalScore.HasValue ? Math.Round(finalScore.Value / maxS * 100, 0) : 0,
            AnsweredCount = dto.Answers.Count,
            ByType = bType.ToDictionary(kv => kv.Key, kv => new QuizTypeStatsDto { Answered = kv.Value.Answered, Total = kv.Value.Total, Score = kv.Value.Score }),
            QuestionResults = qResults,
            Answers = dto.Answers,
            SubmittedAt = studentQuiz.SubmittedAt.ToString("dd MM yy HH:mm")
        };
    }

    public async Task<PracticeQuizDto?> GetPracticeQuizAsync(int studentId, string courseId, int? quizId = null)
    {
        if (!int.TryParse(courseId, out var parsedCourseId))
            return null;

        var course = await Courses.GetByIdAsync(parsedCourseId);
        if (course is null)
            return null;

        var quizSpec = new QuizzesByCourseSpec(parsedCourseId);
        var quizzes = (await Quizzes.GetAllAsync(quizSpec)).ToList();

        Quiz? quiz;
        if (quizId.HasValue)
        {
            quiz = quizzes.FirstOrDefault(q => q.QuizId == quizId.Value);
        }
        else
        {
            var unsubmitted = new List<Quiz>();
            foreach (var q in quizzes)
            {
                var sub = await StudentQuizzes.GetByIdAsync(new StudentQuizSpec(studentId, q.QuizId));
                if (sub is null)
                    unsubmitted.Add(q);
            }

            var pool = unsubmitted.Count > 0 ? unsubmitted : quizzes;
            quiz = pool.Count > 0 ? pool[Random.Shared.Next(pool.Count)] : null;
        }

        if (quiz is null)
            return null;

        var now = DateTime.UtcNow;
        var submission = await StudentQuizzes.GetByIdAsync(new StudentQuizSpec(studentId, quiz.QuizId));
        var isWithinWindow = now >= quiz.StartDate && now <= quiz.DueDate;

        if (submission is null && !isWithinWindow)
            return null;

        var questions = (await QuestionsRepo.GetAllAsync(new QuestionsByQuizSpec(quiz.QuizId))).ToList();

        var tfCount = questions.Count(q => q.Type == "TF");
        var mcqCount = questions.Count(q => q.Type == "MCQ");
        var writtenCount = questions.Count(q => q.Type == "Written");

        QuizSubmitResponseDto? previousSubmission = null;
        if (submission is not null)
        {
            var maxScore = questions.Sum(q => q.Points);
            var deserializedAnswers = submission.AnswersJson is null
                ? new Dictionary<string, object>()
                : JsonSerializer.Deserialize<Dictionary<string, object>>(submission.AnswersJson) ?? new Dictionary<string, object>();

            var deserializedResults = submission.QuestionResultsJson is null
                ? new List<QuestionResultDto>()
                : JsonSerializer.Deserialize<List<QuestionResultDto>>(submission.QuestionResultsJson) ?? new List<QuestionResultDto>();

            var byType = new Dictionary<string, QuizTypeStatsDto>
            {
                ["TF"] = new QuizTypeStatsDto
                {
                    Total = questions.Count(q => q.Type == "TF"),
                    Answered = deserializedResults.Count(r => r.Type == "TF"),
                    Score = deserializedResults.Where(r => r.Type == "TF").Sum(r => r.EarnedPoints)
                },
                ["MCQ"] = new QuizTypeStatsDto
                {
                    Total = questions.Count(q => q.Type == "MCQ"),
                    Answered = deserializedResults.Count(r => r.Type == "MCQ"),
                    Score = deserializedResults.Where(r => r.Type == "MCQ").Sum(r => r.EarnedPoints)
                },
                ["Written Question"] = new QuizTypeStatsDto
                {
                    Total = questions.Count(q => q.Type == "Written"),
                    Answered = deserializedResults.Count(r => r.Type == "Written"),
                    Score = deserializedResults.Where(r => r.Type == "Written").Sum(r => r.EarnedPoints)
                }
            };

            previousSubmission = new QuizSubmitResponseDto
            {
                CourseId = courseId,
                CourseName = course.CourseName,
                Score = submission.Score,
                MaxScore = maxScore,
                Percentage = maxScore > 0 && submission.Score.HasValue ? Math.Round(submission.Score.Value / maxScore * 100, 0) : 0,
                AnsweredCount = deserializedAnswers.Count,
                ByType = byType,
                QuestionResults = deserializedResults,
                Answers = deserializedAnswers,
                SubmittedAt = submission.SubmittedAt.ToString("dd MM yy HH:mm")
            };
        }

        return new PracticeQuizDto
        {
            QuizId = quiz.QuizId,
            CourseId = courseId,
            CourseName = course.CourseName,
            Title = quiz.Title,
            DurationSeconds = quiz.DurationMinutes * 60,
            PageSize = 5,
            MaxAttempts = 1,
            QuestionsSummary = new QuizQuestionsSummaryDto
            {
                Total = questions.Count,
                Tf = tfCount,
                Mcq = mcqCount,
                Written = writtenCount
            },
            PreviousSubmission = previousSubmission,
            Questions = questions.Select((q, i) => new QuizQuestionDto
            {
                Id = "q" + (i + 1).ToString(),
                Type = q.Type,
                Prompt = q.Prompt,
                Options = q.Options is not null
                    ? System.Text.Json.JsonSerializer.Deserialize<List<string>>(q.Options)
                    : null,
                Points = q.Points,
                CorrectAnswer = q.CorrectAnswer
            }).ToList(),
            IsSubmitted = submission is not null
        };
    }

    public async Task<CourseQuizzesDto?> GetQuizzesOverviewAsync(int studentId, string courseId)
    {
        if (!int.TryParse(courseId, out var parsedCourseId))
            return null;

        var course = await Courses.GetByIdAsync(parsedCourseId);
        if (course is null)
            return null;

        var spec = new QuizzesByCourseSpec(parsedCourseId);
        var quizzes = (await Quizzes.GetAllAsync(spec)).ToList();

        var history = new List<QuizHistoryItemDto>();
        var upcoming = new List<QuizUpcomingItemDto>();
        var now = DateTime.UtcNow;

        foreach (var quiz in quizzes)
        {
            var submission = await StudentQuizzes.GetByIdAsync(new StudentQuizSpec(studentId, quiz.QuizId));

            if (submission is not null)
            {
                history.Add(new QuizHistoryItemDto
                {
                    Id = quiz.QuizId.ToString(),
                    Title = quiz.Title,
                    Score = submission.Score,
                    MaxScore = quiz.MaxGrade,
                    DurationMinutes = quiz.DurationMinutes,
                    StartDate = quiz.StartDate,
                    DueDate = quiz.DueDate,
                    Status = "Completed"
                });
            }
            else if (now < quiz.StartDate)
            {
                upcoming.Add(new QuizUpcomingItemDto
                {
                    Id = quiz.QuizId.ToString(),
                    Title = quiz.Title,
                    MaxScore = quiz.MaxGrade,
                    DurationMinutes = quiz.DurationMinutes,
                    StartDate = quiz.StartDate,
                    DueDate = quiz.DueDate,
                    Status = "Upcoming"
                });
            }
            else if (now > quiz.DueDate)
            {
                upcoming.Add(new QuizUpcomingItemDto
                {
                    Id = quiz.QuizId.ToString(),
                    Title = quiz.Title,
                    MaxScore = quiz.MaxGrade,
                    DurationMinutes = quiz.DurationMinutes,
                    StartDate = quiz.StartDate,
                    DueDate = quiz.DueDate,
                    Status = "Missed"
                });
            }
            else
            {
                upcoming.Add(new QuizUpcomingItemDto
                {
                    Id = quiz.QuizId.ToString(),
                    Title = quiz.Title,
                    MaxScore = quiz.MaxGrade,
                    DurationMinutes = quiz.DurationMinutes,
                    StartDate = quiz.StartDate,
                    DueDate = quiz.DueDate,
                    Status = "Active"
                });
            }
        }

        var completed = history.Count;
        var missed = upcoming.Count(u => u.Status == "Missed");
        var upcomingCount = upcoming.Count(u => u.Status == "Upcoming");

        return new CourseQuizzesDto
        {
            CourseId = courseId,
            CourseName = course.CourseName,
            Stats = new QuizStatsDto
            {
                Completed = completed,
                Missed = missed,
                Upcoming = upcomingCount,
                AverageScore = completed > 0 ? history.Where(h => h.Score.HasValue).Select(h => h.Score.Value).DefaultIfEmpty().Average() : 0
            },
            History = history,
            Upcoming = upcoming
        };
    }

    private static (List<QuestionResultDto> Results, Dictionary<string, (int Answered, int Total, decimal Score)> ByType, decimal TotalScore) GradeAnswers(
        List<Question> questions, Dictionary<string, object?> answers)
    {
        var results = new List<QuestionResultDto>();
        var byType = new Dictionary<string, (int Answered, int Total, decimal Score)>();
        decimal totalScore = 0;

        for (var i = 0; i < questions.Count; i++)
        {
            var q = questions[i];
            var key = "q" + (i + 1).ToString();
            var answered = answers.TryGetValue(key, out var raw) && raw?.ToString() is not null;
            var isCorrect = false;
            decimal earned = 0;

            if (answered && q.Type != "Written" && q.CorrectAnswer is not null)
            {
                var studentAnswer = raw?.ToString()?.Trim() ?? "";
                isCorrect = string.Equals(studentAnswer, q.CorrectAnswer, StringComparison.OrdinalIgnoreCase);
                if (isCorrect)
                    earned = q.Points;
            }

            totalScore += earned;

            if (!byType.ContainsKey(q.Type))
                byType[q.Type] = (0, 0, 0);
            var entry = byType[q.Type];
            byType[q.Type] = (entry.Answered + (answered ? 1 : 0), entry.Total + 1, entry.Score + earned);

            results.Add(new QuestionResultDto
            {
                QuestionId = "q" + (i + 1).ToString(),
                Type = q.Type,
                Points = q.Points,
                EarnedPoints = earned,
                IsCorrect = isCorrect && answered
            });
        }

        return (results, byType, totalScore);
    }
}