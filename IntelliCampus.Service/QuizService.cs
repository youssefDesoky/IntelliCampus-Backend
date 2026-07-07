using System.Text.Json;
using IntelliCampus.Domain.Entities;
using IntelliCampus.Domain.Entities.Enums;
using IntelliCampus.Domain.Interfaces;
using IntelliCampus.Service_Abstraction;
using IntelliCampus.shared.Dtos.Quiz;
using IntelliCampus.shared.Pagination;
using IntelliCampus.Service.Specifications;
using IntelliCampus.Service.Exceptions;
using IntelliCampus.Shared.Params;

namespace IntelliCampus.Service;

public class QuizService : IQuizService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly INotificationService _notificationService;
    private readonly IReminderService _reminderService;

    public QuizService(IUnitOfWork unitOfWork, INotificationService notificationService, IReminderService reminderService)
    {
        _unitOfWork = unitOfWork;
        _notificationService = notificationService;
        _reminderService = reminderService;
    }

    private IGenericRepository<Quiz, int> Quizzes
        => _unitOfWork.GetRepository<Quiz, int>();

    private IGenericRepository<StudentQuiz, int> StudentQuizzes
        => _unitOfWork.GetRepository<StudentQuiz, int>();

    private IGenericRepository<Class, int> Classes
        => _unitOfWork.GetRepository<Class, int>();

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

    private IGenericRepository<Question, int> QuestionsRepo
        => _unitOfWork.GetRepository<Question, int>();

    private IGenericRepository<Student, int> Students
        => _unitOfWork.GetRepository<Student, int>();

    private IGenericRepository<Reminder, int> Reminders
        => _unitOfWork.GetRepository<Reminder, int>();

    private IGenericRepository<StudentCourse, (int, int)> StudentCourses
        => _unitOfWork.GetRepository<StudentCourse, (int, int)>();

    public async Task<QuizHistoryItemDto?> GetByIdAsync(int quizId, int studentId)
    {
    
        var spec = new QuizSpec(quizId);
        var quiz = await Quizzes.GetByIdAsync(spec);
        if (quiz is null) 
            throw new QuizNotFoundException(quizId);

        var submission = await StudentQuizzes.GetByIdAsync(new StudentQuizSpec(studentId, quizId));
        var hasSubmission = submission is not null;
        var now = EgyptTime.Now;

        return new QuizHistoryItemDto
        {
            Id = quiz.QuizId.ToString(),
            Title = quiz.Title,
            Description = quiz.Description,
            Score = submission?.Score,
            MaxScore = quiz.MaxGrade,
            DurationMinutes = quiz.DurationMinutes,
            StartDate = quiz.StartDate,
            DueDate = quiz.DueDate,
            Status = hasSubmission ? "Completed" : quiz.DueDate < now ? "Overdue" :
                     quiz.StartDate > now ? "Upcoming" : "Active"
        };
    }

    public async Task<QuizHistoryItemDto?> GetByIdInCourseAsync(int quizId, int studentId, string courseId)
    {
        if (!int.TryParse(courseId, out var parsedCourseId))
            throw new CourseNotFoundException(courseId);

        var course = await Courses.GetByIdAsync(parsedCourseId);
        if (course is null)
            throw new CourseNotFoundException(parsedCourseId);

        var spec = new QuizSpec(quizId);
        var quiz = await Quizzes.GetByIdAsync(spec);
        if (quiz is null) 
            throw new QuizNotFoundException(quizId);

        if (quiz.CourseId != parsedCourseId)
            throw new QuizNotFoundException(quizId);

        var submission = await StudentQuizzes.GetByIdAsync(new StudentQuizSpec(studentId, quizId));
        var hasSubmission = submission is not null;
        var now = EgyptTime.Now;

    return new QuizHistoryItemDto
    {
        Id = quiz.QuizId.ToString(),
        Title = quiz.Title,
        Description = quiz.Description,
        Score = submission?.Score,
            MaxScore = quiz.MaxGrade,
            DurationMinutes = quiz.DurationMinutes,
            StartDate = quiz.StartDate,
            DueDate = quiz.DueDate,
            Status = hasSubmission ? "Completed" : quiz.DueDate < now ? "Overdue" :
                     quiz.StartDate > now ? "Upcoming" : "Active"
        };
    }

    public async Task<IEnumerable<QuizDto>> GetByCourseIdAsync(int courseId)
    {
        var course = await Courses.GetByIdAsync(courseId);
        if (course is null)
            throw new CourseNotFoundException(courseId);

        var spec = new QuizSpec(courseId, byCourse: true);
        var quizzes = await Quizzes.GetAllAsync(spec, asNoTracking: true);
        return quizzes.Select(MapToDto);
    }

    public async Task<QuizDto> CreateAsync(int instructorId, CreateQuizDto dto)
    {
        var course = await Courses.GetByIdAsync(dto.CourseId);
        if (course is null)
            throw new CourseNotFoundException(dto.CourseId);

        if (course.Status != CourseStatus.Active)
            throw new InvalidOperationException("This course is finalized and read-only.");

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
        if (quiz is null)
            throw new QuizNotFoundException(quizId);

        await EnsureCourseActiveAsync(quiz.CourseId);

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
            throw new CourseNotFoundException(courseId);

        var course = await Courses.GetByIdAsync(parsedCourseId);
        if (course is null)
            throw new CourseNotFoundException(parsedCourseId);

        if (course.Status != CourseStatus.Active)
            throw new InvalidOperationException("This course is finalized and read-only.");

        var teachesCourse = await Classes.AnyAsync(c => c.CourseId == parsedCourseId && c.InstructorId == instructorId);
        if (!teachesCourse)
            throw new InvalidOperationException("Not authorized to create quizzes for this course.");

        var quiz = new Quiz
        {
            Title = dto.Title,
            Description = dto.Description,
            StartDate = dto.StartDate,
            DueDate = dto.StartDate.AddMinutes(dto.DurationMinutes),
            DurationMinutes = dto.DurationMinutes,
            MaxGrade = dto.MaxGrade,
            TotalMarks = (int)dto.MaxGrade,
            CourseId = parsedCourseId
        };

        Quizzes.Add(quiz);
        await _unitOfWork.SaveChangesAsync();

        var registered = (await StudentCourses.GetAllAsync(
            new StudentCourseIdsSpec(parsedCourseId, true, StudentCourseStatus.InProgress), asNoTracking: true))
            .Select(sc => sc.StudentId)
            .Distinct()
            .ToList();

        foreach (var studentId in registered)
        {
            Reminders.Add(new Reminder
            {
                StudentId = studentId,
                Title = $"Quiz due: {quiz.Title}",
                Date = quiz.DueDate,
                Type = ReminderType.Quiz,
                Location = null,
                Priority = "medium"
            });
        }

        await _unitOfWork.SaveChangesAsync();

        if (registered.Count > 0)
        {
            await _notificationService.SendToManyAsync(
                registered,
                NotificationType.NewQuizPosted,
                $"A new quiz \"{quiz.Title}\" has been posted. Due: {quiz.DueDate:g}",
                "New Quiz Available",
                $"/courses/{parsedCourseId}/quizzes");
        }

        var spec = new QuizSpec(quiz.QuizId);
        var result = await Quizzes.GetByIdAsync(spec);
        return MapToDto(result!);
    }

    public async Task<bool> DeleteInCourseAsync(int quizId, int instructorId, string courseId)
    {
        if (!int.TryParse(courseId, out var parsedCourseId))
            throw new CourseNotFoundException(courseId);

        var course = await Courses.GetByIdAsync(parsedCourseId);
        if (course is null)
            throw new CourseNotFoundException(parsedCourseId);

        if (course.Status != CourseStatus.Active)
            throw new InvalidOperationException("This course is finalized and read-only.");

        var spec = new QuizSpec(quizId);
        var quiz = await Quizzes.GetByIdAsync(spec);
        if (quiz is null)
            throw new QuizNotFoundException(quizId);

        if (quiz.CourseId != parsedCourseId)
            throw new QuizNotFoundException(quizId);

        var teachesCourse = await Classes.AnyAsync(c => c.CourseId == quiz.CourseId && c.InstructorId == instructorId);
        if (!teachesCourse)
            throw new InvalidOperationException("Not authorized.");

        Quizzes.Delete(quiz);
        await _unitOfWork.SaveChangesAsync();
        return true;
    }

    public async Task<QuizDto> UpdateInCourseAsync(int quizId, int instructorId, string courseId, UpdateQuizDto dto)
    {
        if (!int.TryParse(courseId, out var parsedCourseId))
            throw new CourseNotFoundException(courseId);

        var course = await Courses.GetByIdAsync(parsedCourseId);
        if (course is null)
            throw new CourseNotFoundException(parsedCourseId);

        if (course.Status != CourseStatus.Active)
            throw new InvalidOperationException("This course is finalized and read-only.");

        var spec = new QuizSpec(quizId);
        var quiz = await Quizzes.GetByIdAsync(spec);
        if (quiz is null)
            throw new QuizNotFoundException(quizId);

        if (quiz.CourseId != parsedCourseId)
            throw new QuizNotFoundException(quizId);

        var teachesCourse = await Classes.AnyAsync(c => c.CourseId == quiz.CourseId && c.InstructorId == instructorId);
        if (!teachesCourse)
            throw new InvalidOperationException("Not authorized.");

        if (dto.Title is not null) quiz.Title = dto.Title;
        if (dto.Description is not null) quiz.Description = dto.Description;
        if (dto.StartDate.HasValue) quiz.StartDate = dto.StartDate.Value;
        if (dto.DurationMinutes.HasValue) quiz.DurationMinutes = dto.DurationMinutes.Value;

        quiz.DueDate = dto.StartDate.HasValue || dto.DurationMinutes.HasValue
            ? quiz.StartDate.AddMinutes(quiz.DurationMinutes)
            : quiz.DueDate;
        if (dto.MaxGrade.HasValue)
        {
            var existingQuestions = await QuestionsRepo.GetAllAsync(new QuestionsByQuizSpec(quizId), asNoTracking: true);
            var existingPoints = existingQuestions.Sum(q => q.Points);
            if (existingPoints > 0 && dto.MaxGrade.Value != existingPoints)
                throw new InvalidOperationException($"Max grade must equal the total question points ({existingPoints}). Got: {dto.MaxGrade.Value}.");
            quiz.MaxGrade = dto.MaxGrade.Value;
            quiz.TotalMarks = (int)dto.MaxGrade.Value;
        }

        Quizzes.Update(quiz);
        await _unitOfWork.SaveChangesAsync();

        var reloaded = await Quizzes.GetByIdAsync(spec);
        return MapToDto(reloaded!);
    }

    public async Task AddQuestionsAsync(int quizId, int instructorId, string courseId, List<CreateQuestionDto> questions)
    {
        if (!int.TryParse(courseId, out var parsedCourseId))
            throw new CourseNotFoundException(courseId);

        var course = await Courses.GetByIdAsync(parsedCourseId);
        if (course is null)
            throw new CourseNotFoundException(parsedCourseId);

        var spec = new QuizSpec(quizId);
        var quiz = await Quizzes.GetByIdAsync(spec);
        if (quiz is null)
            throw new QuizNotFoundException(quizId);

        if (quiz.CourseId != parsedCourseId)
            throw new QuizNotFoundException(quizId);

        await EnsureCourseActiveAsync(quiz.CourseId);

        var teachesCourse = await Classes.AnyAsync(c => c.CourseId == quiz.CourseId && c.InstructorId == instructorId);
        if (!teachesCourse)
            throw new InvalidOperationException("Not authorized.");

        var existingQuestions = await QuestionsRepo.GetAllAsync(new QuestionsByQuizSpec(quizId), asNoTracking: true);
        var existingPoints = existingQuestions.Sum(q => q.Points);
        var newPoints = questions.Sum(q => q.Points);
        var totalPoints = existingPoints + newPoints;

        if (totalPoints != quiz.MaxGrade)
        {
            var diff = totalPoints - quiz.MaxGrade;
            var direction = diff > 0 ? "more" : "less";
            throw new InvalidOperationException($"Quiz max grade is {quiz.MaxGrade} but questions total {totalPoints} ({Math.Abs(diff)} points {direction}).");
        }

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

    public async Task<List<object>> GetQuestionsAsync(int quizId, int instructorId, string courseId)
    {
        if (!int.TryParse(courseId, out var parsedCourseId))
            throw new CourseNotFoundException(courseId);

        var course = await Courses.GetByIdAsync(parsedCourseId);
        if (course is null)
            throw new CourseNotFoundException(parsedCourseId);

        var spec = new QuizSpec(quizId);
        var quiz = await Quizzes.GetByIdAsync(spec);
        if (quiz is null || quiz.CourseId != parsedCourseId)
            throw new QuizNotFoundException(quizId);

        var teachesCourse = await Classes.AnyAsync(c => c.CourseId == quiz.CourseId && c.InstructorId == instructorId);
        if (!teachesCourse)
            throw new InvalidOperationException("Not authorized.");

        var questions = await QuestionsRepo.GetAllAsync(new QuestionsByQuizSpec(quizId), asNoTracking: true);
        return questions.Select(q => new
        {
            Id = q.Id,
            QuizId = q.QuizId,
            Type = q.Type,
            Prompt = q.Prompt,
            Options = q.Options is not null ? System.Text.Json.JsonSerializer.Deserialize<List<string>>(q.Options) : null,
            Points = q.Points,
            CorrectAnswer = q.CorrectAnswer
        }).Select(x => (object)x).ToList();
    }

    public async Task DeleteQuestionAsync(int questionId, int instructorId, string courseId)
    {
        if (!int.TryParse(courseId, out var parsedCourseId))
            throw new CourseNotFoundException(courseId);

        var course = await Courses.GetByIdAsync(parsedCourseId);
        if (course is null)
            throw new CourseNotFoundException(parsedCourseId);

        if (course.Status != CourseStatus.Active)
            throw new InvalidOperationException("This course is finalized and read-only.");

        var question = await QuestionsRepo.GetByIdAsync(questionId);
        if (question is null)
            throw new QuestionNotFoundException(questionId);

        var spec = new QuizSpec(question.QuizId);
        var quiz = await Quizzes.GetByIdAsync(spec);
        if (quiz is null || quiz.CourseId != parsedCourseId)
            throw new QuizNotFoundException(question.QuizId);

        var teachesCourse = await Classes.AnyAsync(c => c.CourseId == quiz.CourseId && c.InstructorId == instructorId);
        if (!teachesCourse)
            throw new InvalidOperationException("Not authorized.");

        QuestionsRepo.Delete(question);
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task<List<StudentSubmissionDto>> GetSubmissionsAsync(int quizId, int instructorId, string courseId)
    {
        if (!int.TryParse(courseId, out var parsedCourseId))
            throw new CourseNotFoundException(courseId);

        var course = await Courses.GetByIdAsync(parsedCourseId);
        if (course is null)
            throw new CourseNotFoundException(parsedCourseId);

        var spec = new QuizSpec(quizId);
        var quiz = await Quizzes.GetByIdAsync(spec);
        if (quiz is null || quiz.CourseId != parsedCourseId)
            throw new QuizNotFoundException(quizId);

        var teachesCourse = await Classes.AnyAsync(c => c.CourseId == quiz.CourseId && c.InstructorId == instructorId);
        if (!teachesCourse)
            throw new InvalidOperationException("Not authorized.");

        var allQuestions = (await QuestionsRepo.GetAllAsync(new QuestionsByQuizSpec(quizId), asNoTracking: true)).ToList();
        var maxScore = allQuestions.Sum(q => q.Points);

        var subsSpec = new StudentQuizSpec(quizId, allResults: true);
        var submissions = await StudentQuizzes.GetAllAsync(subsSpec, asNoTracking: true);

        return submissions.Select(sq =>
        {
            var answers = sq.AnswersJson is not null
                ? JsonSerializer.Deserialize<Dictionary<string, object>>(sq.AnswersJson)
                : null;
            var results = sq.QuestionResultsJson is not null
                ? JsonSerializer.Deserialize<List<QuestionResultDto>>(sq.QuestionResultsJson)
                : null;
            var resultsByQId = results?.ToDictionary(r => r.QuestionId) ?? new();

            var answerDetails = new List<SubmissionAnswerDetailDto>();
            for (int i = 0; i < allQuestions.Count; i++)
            {
                var q = allQuestions[i];
                var qId = "q" + (i + 1);
                var result = resultsByQId.GetValueOrDefault(qId);
                answerDetails.Add(new SubmissionAnswerDetailDto
                {
                    QuestionId = qId,
                    Type = q.Type,
                    Prompt = q.Prompt,
                    Options = q.Options is not null ? JsonSerializer.Deserialize<List<string>>(q.Options) : null,
                    Points = q.Points,
                    StudentAnswer = answers?.TryGetValue(qId, out var val) == true ? val?.ToString() : null,
                    CorrectAnswer = q.CorrectAnswer,
                    AutoScore = result?.EarnedPoints,
                    Score = result?.EarnedPoints
                });
            }

            return new StudentSubmissionDto
            {
                StudentId = sq.StudentId,
                StudentName = sq.Student?.User?.FullName,
                Score = sq.Score,
                MaxScore = maxScore,
                SubmittedAt = sq.SubmittedAt.ToString("dd MM yy HH:mm"),
                Answers = answers,
                QuestionResults = results,
                AnswerDetails = answerDetails
            };
        }).ToList();
    }

    public async Task GradeWrittenAsync(int quizId, int studentId, int instructorId, string courseId, GradeWrittenDto dto)
    {
        if (!int.TryParse(courseId, out var parsedCourseId))
            throw new CourseNotFoundException(courseId);

        var course = await Courses.GetByIdAsync(parsedCourseId);
        if (course is null)
            throw new CourseNotFoundException(parsedCourseId);

        var spec = new QuizSpec(quizId);
        var quiz = await Quizzes.GetByIdAsync(spec);
        if (quiz is null || quiz.CourseId != parsedCourseId)
            throw new QuizNotFoundException(quizId);

        var teachesCourse = await Classes.AnyAsync(c => c.CourseId == quiz.CourseId && c.InstructorId == instructorId);
        if (!teachesCourse)
            throw new InvalidOperationException("Not authorized.");

        await EnsureStudentEnrollmentActiveAsync(studentId, quiz.CourseId);

        var existing = await StudentQuizzes.GetByIdAsync(new StudentQuizSpec(studentId, quizId));
        if (existing is null)
            throw new SubmissionNotFoundException(studentId, quizId);

        var allQuestions = (await QuestionsRepo.GetAllAsync(new QuestionsByQuizSpec(quizId), asNoTracking: true)).ToList();
        var existingResults = existing.QuestionResultsJson is not null
            ? JsonSerializer.Deserialize<List<QuestionResultDto>>(existing.QuestionResultsJson)
            : null;

        decimal newTotal = 0;
        if (existingResults is not null)
        {
            foreach (var r in existingResults)
            {
                if (dto.QuestionScores.TryGetValue(r.QuestionId, out var manualScore))
                {
                    var qIdx = int.Parse(r.QuestionId.Substring(1)) - 1;
                    if (qIdx >= 0 && qIdx < allQuestions.Count && manualScore > allQuestions[qIdx].Points)
                        throw new InvalidOperationException(
                            $"Score for question {r.QuestionId} ({allQuestions[qIdx].Points} pts) cannot exceed its maximum points ({allQuestions[qIdx].Points}).");
                    r.EarnedPoints = manualScore;
                }
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
        if (result is null)
            throw new SubmissionNotFoundException(studentId, quizId);
        return MapResultToDto(result);
    }

    public async Task<IEnumerable<StudentQuizDto>> GetAllResultsAsync(int quizId, int instructorId)
    {
        var quiz = await Quizzes.GetByIdAsync(quizId);
        if (quiz is null)
            throw new QuizNotFoundException(quizId);

        var teachesCourse = await Classes.AnyAsync(c => c.CourseId == quiz.CourseId && c.InstructorId == instructorId);
        if (!teachesCourse)
            throw new InvalidOperationException("Not authorized.");

        var spec = new StudentQuizSpec(quizId, allResults: true);
        var results = await StudentQuizzes.GetAllAsync(spec, asNoTracking: true);
        return results.Select(MapResultToDto);
    }

    public async Task<IEnumerable<StudentQuizDto>> GetByStudentIdAsync(int studentId)
    {
        var student = await Students.GetByIdAsync(studentId);
        if (student is null)
            throw new StudentNotFoundException(studentId);

        var spec = new StudentQuizSpec(studentId, byStudent: true, dummy: true);
        var results = await StudentQuizzes.GetAllAsync(spec, asNoTracking: true);
        return results.Select(MapResultToDto);
    }

    private static QuizDto MapToDto(Quiz q)
    {
        var now = EgyptTime.Now;
        return new QuizDto
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
            Status = q.DueDate < now ? "Completed" :
                     q.StartDate > now ? "Upcoming" : "Active"
        };
    }

    private static StudentQuizDto MapResultToDto(StudentQuiz sq) => new()
    {
        StudentId = sq.StudentId,
        StudentName = sq.Student?.User?.FullName,
        QuizId = sq.QuizId,
        QuizTitle = sq.Quiz?.Title,
        Score = sq.Score,
        MaxGrade = sq.Quiz?.MaxGrade ?? 0,
        SubmittedAt = sq.SubmittedAt.ToString("dd MM yy HH:mm"),
        IsLate = sq.IsLate
    };

    public async Task<QuizSubmitResponseDto?> SubmitPracticeQuizAsync(int studentId, string courseId, SubmitQuizDto dto)
    {
        var (course, quiz, allQ, qResults, bType, now, existing, answersJson, resultsJson) = await GradePracticeSubmission(studentId, courseId, dto);
        var response = await BuildSubmitResponse(studentId, courseId, dto, course, quiz, allQ, qResults, bType, now, existing, answersJson, resultsJson);
        await _reminderService.MarkSubmissionCompletedAsync(studentId, ReminderType.Quiz, quiz.DueDate);
        return response;
    }

    public async Task<PracticeQuizDto?> GetPracticeQuizAsync(int studentId, string courseId, QuizQueryParams queryParams)
    {
        var (quiz, questions, submission, course, now) = await LoadQuizQuestionsAsync(studentId, courseId, queryParams);
        return BuildPracticeDto(studentId, courseId, quiz, questions, submission, course, now);
    }

    public async Task<CourseQuizzesDto?> GetQuizzesOverviewAsync(int studentId, string courseId)
    {
        var (course, quizzes) = await LoadCourseQuizDataAsync(courseId);
        return await BuildOverviewDto(studentId, courseId, course, quizzes);
    }

    public async Task<PaginatedResult<CourseQuizzesDto>> GetQuizzesOverviewAsync(int studentId, string courseId, QuizQueryParams queryParams)
    {
        var result = await GetQuizzesOverviewAsync(studentId, courseId);
        var wrapped = result is not null ? new List<CourseQuizzesDto> { result } : [];
        return new PaginatedResult<CourseQuizzesDto>(queryParams.PageIndex, wrapped.Count, wrapped.Count, wrapped);
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

    private async Task<(Quiz quiz, List<Question> questions, StudentQuiz submission, Course course, DateTime now)> LoadQuizQuestionsAsync(int studentId, string courseId, QuizQueryParams queryParams)
    {
        if (!int.TryParse(courseId, out var parsedCourseId))
            throw new CourseNotFoundException(courseId);

        var course = await Courses.GetByIdAsync(parsedCourseId);
        if (course is null)
            throw new CourseNotFoundException(parsedCourseId);

        var quizSpec = new QuizSpec(queryParams, parsedCourseId);
        var quizzes = (await Quizzes.GetAllAsync(quizSpec, asNoTracking: true)).ToList();

        Quiz? quiz;
        if (queryParams.QuizId.HasValue)
        {
            quiz = quizzes.FirstOrDefault();
        }
        else
        {
            var quizIds = quizzes.Select(q => q.QuizId).Distinct().ToHashSet();
            var allSubmissions = quizIds.Count > 0
                ? await StudentQuizzes.GetAllAsync(new StudentQuizSpec(studentId, quizIds), asNoTracking: true)
                : new List<StudentQuiz>();
            var submittedIds = allSubmissions.Select(s => s.QuizId).ToHashSet();
            var unsubmitted = quizzes.Where(q => !submittedIds.Contains(q.QuizId)).ToList();

            var pool = unsubmitted.Count > 0 ? unsubmitted : quizzes;
            quiz = pool.Count > 0 ? pool[Random.Shared.Next(pool.Count)] : null;
        }

        if (quiz is null)
            throw new QuizNotFoundException(queryParams.QuizId ?? 0);

        var now = EgyptTime.Now;
        var quizEndTime = quiz.StartDate.AddMinutes(quiz.DurationMinutes);
        var isWithinWindow = now >= quiz.StartDate && now <= quizEndTime;

        var submission = await StudentQuizzes.GetByIdAsync(new StudentQuizSpec(studentId, quiz.QuizId));

        if (submission is null)
        {
            if (!isWithinWindow)
                throw new InvalidOperationException("Quiz is not available at this time.");

            submission = new StudentQuiz
            {
                StudentId = studentId,
                QuizId = quiz.QuizId,
                StartedAt = now
            };
            StudentQuizzes.Add(submission);
            await _unitOfWork.SaveChangesAsync();
        }
        else if (submission.StartedAt.HasValue && submission.Score is null)
        {
            var timeLimit = submission.StartedAt.Value.AddMinutes(quiz.DurationMinutes);
            if (now > timeLimit)
                throw new InvalidOperationException("Quiz time has expired.");
        }

        var questions = (await QuestionsRepo.GetAllAsync(new QuestionsByQuizSpec(quiz.QuizId), asNoTracking: true)).ToList();

        return (quiz, questions, submission, course, now);
    }

    private static PracticeQuizDto BuildPracticeDto(int studentId, string courseId, Quiz quiz, List<Question> questions, StudentQuiz submission, Course course, DateTime now)
    {
        var tfCount = questions.Count(q => q.Type == "TF");
        var mcqCount = questions.Count(q => q.Type == "MCQ");
        var writtenCount = questions.Count(q => q.Type == "Written");

        QuizSubmitResponseDto? previousSubmission = null;
        if (submission is not null && submission.SubmittedAt != default)
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

        var remainingSeconds = submission.SubmittedAt != default
            ? 0
            : Math.Max(0, (int)((submission.StartedAt ?? now).AddMinutes(quiz.DurationMinutes) - now).TotalSeconds);

        return new PracticeQuizDto
        {
            QuizId = quiz.QuizId,
            CourseId = courseId,
            CourseName = course.CourseName,
            Title = quiz.Title,
            DurationSeconds = remainingSeconds,
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

    private async Task<(Course course, List<Quiz> quizzes)> LoadCourseQuizDataAsync(string courseId)
    {
        if (!int.TryParse(courseId, out var parsedCourseId))
            throw new CourseNotFoundException(courseId);

        var course = await Courses.GetByIdAsync(parsedCourseId);
        if (course is null)
            throw new CourseNotFoundException(parsedCourseId);

        var spec = new QuizzesByCourseSpec(parsedCourseId);
        var quizzes = (await Quizzes.GetAllAsync(spec, asNoTracking: true)).ToList();

        return (course, quizzes);
    }

    private async Task<CourseQuizzesDto> BuildOverviewDto(int studentId, string courseId, Course course, List<Quiz> quizzes)
    {
        var history = new List<QuizHistoryItemDto>();
        var upcoming = new List<QuizUpcomingItemDto>();
        var now = EgyptTime.Now;

        var quizIds = quizzes.Select(q => q.QuizId).ToHashSet();
        var submissions = quizIds.Count > 0
            ? (await StudentQuizzes.GetAllAsync(new StudentQuizSpec(studentId, quizIds), asNoTracking: true))
                .ToDictionary(s => s.QuizId)
            : new Dictionary<int, StudentQuiz>();

        foreach (var quiz in quizzes)
        {
            var submission = submissions.GetValueOrDefault(quiz.QuizId);

            if (submission is not null && submission.Score is not null)
            {
                history.Add(new QuizHistoryItemDto
                {
                    Id = quiz.QuizId.ToString(),
                    Title = quiz.Title,
                    Description = quiz.Description,
                    Score = submission.Score,
                    MaxScore = quiz.MaxGrade,
                    DurationMinutes = quiz.DurationMinutes,
                    StartDate = quiz.StartDate,
                    DueDate = quiz.DueDate,
                    Status = "Completed"
                });
            }
            else if (submission is not null && submission.Score is null)
            {
                var timeLimit = submission.StartedAt.HasValue
                    ? submission.StartedAt.Value.AddMinutes(quiz.DurationMinutes)
                    : quiz.DueDate;

                if (now <= timeLimit)
                {
                    upcoming.Add(new QuizUpcomingItemDto
                    {
                        Id = quiz.QuizId.ToString(),
                        Title = quiz.Title,
                        Description = quiz.Description,
                        MaxScore = quiz.MaxGrade,
                        DurationMinutes = quiz.DurationMinutes,
                        StartDate = quiz.StartDate,
                        DueDate = quiz.DueDate,
                        Status = "Active"
                    });
                }
                else
                {
                    upcoming.Add(new QuizUpcomingItemDto
                    {
                        Id = quiz.QuizId.ToString(),
                        Title = quiz.Title,
                        Description = quiz.Description,
                        MaxScore = quiz.MaxGrade,
                        DurationMinutes = quiz.DurationMinutes,
                        StartDate = quiz.StartDate,
                        DueDate = quiz.DueDate,
                        Status = "Missed"
                    });
                }
            }
            else if (now < quiz.StartDate)
            {
                upcoming.Add(new QuizUpcomingItemDto
                {
                    Id = quiz.QuizId.ToString(),
                    Title = quiz.Title,
                    Description = quiz.Description,
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
                    Description = quiz.Description,
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
                    Description = quiz.Description,
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

    private async Task<(Course course, Quiz quiz, List<Question> allQ, List<QuestionResultDto> qResults, Dictionary<string, (int Answered, int Total, decimal Score)> bType, DateTime now, StudentQuiz? existing, string answersJson, string resultsJson)> GradePracticeSubmission(int studentId, string courseId, SubmitQuizDto dto)
    {
        if (!int.TryParse(courseId, out var parsedCourseId))
            throw new CourseNotFoundException(courseId);

        var course = await Courses.GetByIdAsync(parsedCourseId);
        if (course is null)
            throw new CourseNotFoundException(parsedCourseId);

        var quiz = await Quizzes.GetByIdAsync(dto.QuizId);
        if (quiz is null)
            throw new QuizNotFoundException(dto.QuizId);

        var quizCourse = await Courses.GetByIdAsync(quiz.CourseId);
        if (quizCourse is null || quizCourse.Status != CourseStatus.Active)
            throw new InvalidOperationException("This course is finalized and read-only.");

        await EnsureStudentEnrollmentActiveAsync(studentId, quiz.CourseId);

        var now = EgyptTime.Now;
        var quizEndTime = quiz.StartDate.AddMinutes(quiz.DurationMinutes);
        if (now < quiz.StartDate || now > quizEndTime)
            throw new InvalidOperationException("Quiz is not available for submission at this time.");

        var existing = await StudentQuizzes.GetByIdAsync(new StudentQuizSpec(studentId, quiz.QuizId));
        if (existing is not null && existing.StartedAt.HasValue)
        {
            var timeLimit = existing.StartedAt.Value.AddMinutes(quiz.DurationMinutes);
            if (now > timeLimit)
                throw new InvalidOperationException("Quiz time has expired.");
        }

        var allQ = (await QuestionsRepo.GetAllAsync(new QuestionsByQuizSpec(quiz.QuizId), asNoTracking: true)).ToList();
        var (qResults, bType, _) = GradeAnswers(allQ, dto.Answers);

        var answersJson = JsonSerializer.Serialize(dto.Answers);
        var resultsJson = JsonSerializer.Serialize(qResults);

        return (course, quiz, allQ, qResults, bType, now, existing, answersJson, resultsJson);
    }

    private async Task<QuizSubmitResponseDto> BuildSubmitResponse(int studentId, string courseId, SubmitQuizDto dto, Course course, Quiz quiz, List<Question> allQ, List<QuestionResultDto> qResults, Dictionary<string, (int Answered, int Total, decimal Score)> bType, DateTime now, StudentQuiz? existing, string answersJson, string resultsJson)
    {
        decimal? finalScore = qResults.Sum(q => q.EarnedPoints);

        if (existing is not null)
        {
            existing.Score = finalScore;
            existing.StartedAt ??= now;
            existing.SubmittedAt = now;
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
            StartedAt = now,
            SubmittedAt = now,
            IsLate = now > quiz.DueDate,
            AnswersJson = answersJson,
            QuestionResultsJson = resultsJson
        };
        StudentQuizzes.Add(studentQuiz);
        await _unitOfWork.SaveChangesAsync();

        await _notificationService.SendAsync(
            studentId,
            NotificationType.QuizSubmitted,
            $"Your quiz '{quiz.Title}' was submitted successfully.",
            clickUrl: $"/courses/{courseId}/quizzes/practice?quizId={quiz.QuizId}&review=graded");

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
}