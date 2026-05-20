using IntelliCampus.Service_Abstraction;
using IntelliCampus.Shared.Dtos.Quiz;
using IntelliCampus.Domain.Entities;
using IntelliCampus.Domain.Entities.Enums;
using IntelliCampus.Domain.Interfaces;
using IntelliCampus.Service.Specifications;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace IntelliCampus.Service
{
    public class QuizService : IQuizService
    {
        private readonly IUnitOfWork _unitOfWork;

        public QuizService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<CourseQuizzesDto> GetCourseQuizzesAsync(int courseId, int studentId)
        {
            var course = await _unitOfWork.GetRepository<Course, int>().GetByIdAsync(courseId);
            if (course == null)
            {
                return null;
            }

            var quizSpec = new QuizByCourseIdSpec(courseId);
            var quizzes = await _unitOfWork.GetRepository<Quiz, int>().GetAllAsync(quizSpec);

            var studentQuizSpec = new StudentQuizByStudentAndQuizzesSpec(studentId, quizzes.Select(q => q.Id));
            var studentQuizzes = await _unitOfWork.GetRepository<StudentQuiz, int>().GetAllAsync(studentQuizSpec);

            var history = new List<QuizDto>();
            var upcoming = new List<QuizDto>();

            foreach (var quiz in quizzes)
            {
                var studentQuiz = studentQuizzes.FirstOrDefault(sq => sq.QuizId == quiz.Id);
                var status = "Not Attempted";
                double? score = null;

                if (studentQuiz != null)
                {
                    status = "Attempted";
                    score = (double?)studentQuiz.Score;
                }

                var quizDto = new QuizDto
                {
                    Id = quiz.Id,
                    Title = quiz.Title,
                    Score = score,
                    MaxScore = quiz.MaxScore,
                    Deadline = quiz.Deadline,
                    Status = status
                };

                if (quiz.Deadline < DateTime.UtcNow || studentQuiz != null)
                {
                    history.Add(quizDto);
                }
                else
                {
                    upcoming.Add(quizDto);
                }
            }

            var completedCount = history.Count(q => q.Status == "Attempted");
            var missedCount = history.Count(q => q.Status == "Not Attempted" && q.Deadline < DateTime.UtcNow);
            var totalScore = history.Where(q => q.Score.HasValue).Sum(q => q.Score.Value);
            var averageScore = completedCount > 0 ? totalScore / completedCount : 0;

            var stats = new QuizStatsDto
            {
                Completed = completedCount,
                Missed = missedCount,
                Upcoming = upcoming.Count,
                AverageScore = averageScore
            };

            return new CourseQuizzesDto
            {
                CourseId = course.CourseId.ToString(),
                CourseName = course.CourseName,
                History = history,
                Upcoming = upcoming,
                Stats = stats
            };
        }

        public async Task<QuizSubmissionResultDto?> GradeQuizSubmissionAsync(int instructorId, GradeQuizSubmissionDto dto)
        {
            var studentQuizSpec = new StudentQuizByStudentAndQuizzesSpec(dto.StudentId, new[] { dto.QuizId });
            var submissions = await _unitOfWork.GetRepository<StudentQuiz, int>().GetAllAsync(studentQuizSpec);
            var submission = submissions.FirstOrDefault();
            if (submission is null) return null;

            var quiz = await _unitOfWork.GetRepository<Quiz, int>().GetByIdAsync(dto.QuizId);
            if (quiz is null) return null;

            var classes = (await _unitOfWork.GetRepository<Class, int>().GetAllAsync())
                .Where(c => c.CourseId == quiz.CourseId && c.InstructorId == instructorId)
                .ToList();
            if (classes.Count == 0)
                throw new InvalidOperationException("Not authorized.");

            var questionSpec = new QuestionsByQuizSpec(dto.QuizId);
            var questions = (await _unitOfWork.GetRepository<Question, int>().GetAllAsync(questionSpec)).ToList();

            var answers = string.IsNullOrEmpty(submission.Answers)
                ? new List<StudentAnswerJson>()
                : JsonSerializer.Deserialize<List<StudentAnswerJson>>(submission.Answers) ?? new();

            double autoScore = 0;
            double manualScore = 0;
            var results = new List<QuestionResultDto>();

            foreach (var question in questions)
            {
                var studentAnswer = answers.FirstOrDefault(a => a.QuestionId == question.Id);
                var answerText = studentAnswer?.Answer ?? "";

                if (question.Type != QuestionType.Written)
                {
                    var isCorrect = string.Equals(answerText, question.CorrectAnswer, StringComparison.OrdinalIgnoreCase);
                    if (isCorrect)
                        autoScore += question.Points;

                    results.Add(new QuestionResultDto
                    {
                        QuestionId = question.Id,
                        Type = question.Type.ToString().ToLower(),
                        Prompt = question.Prompt,
                        StudentAnswer = answerText,
                        CorrectAnswer = question.CorrectAnswer,
                        IsCorrect = isCorrect,
                        Points = question.Points,
                        Earned = isCorrect ? question.Points : 0,
                        Feedback = null
                    });
                }
                else
                {
                    var writtenScore = dto.WrittenScores?.FirstOrDefault(ws => ws.QuestionId == question.Id);
                    var earned = writtenScore?.Score ?? 0;
                    if (earned > question.Points)
                        throw new InvalidOperationException($"Score for question {question.Id} cannot exceed {question.Points}.");

                    manualScore += earned;

                    results.Add(new QuestionResultDto
                    {
                        QuestionId = question.Id,
                        Type = "written",
                        Prompt = question.Prompt,
                        StudentAnswer = answerText,
                        CorrectAnswer = question.CorrectAnswer,
                        IsCorrect = earned >= question.Points,
                        Points = question.Points,
                        Earned = earned,
                        Feedback = writtenScore?.Feedback
                    });
                }
            }

            var totalScore = autoScore + manualScore;
            submission.Score = (decimal)totalScore;
            submission.GradedByInstructorId = instructorId;
            submission.GradedAt = DateTime.UtcNow;

            _unitOfWork.GetRepository<StudentQuiz, int>().Update(submission);
            await _unitOfWork.SaveChangesAsync();

            return new QuizSubmissionResultDto
            {
                StudentId = dto.StudentId,
                QuizId = dto.QuizId,
                QuizTitle = quiz.Title,
                TotalScore = totalScore,
                MaxScore = questions.Sum(q => q.Points),
                AutoGradedScore = autoScore,
                ManualScore = manualScore,
                GradedAt = submission.GradedAt,
                QuestionResults = results
            };
        }

        public async Task<QuizStartDto> GetQuizStartAsync(int quizId, int studentId)
        {
            var quiz = await _unitOfWork.GetRepository<Quiz, int>().GetByIdAsync(quizId);
            if (quiz == null)
            {
                return null;
            }

            var course = await _unitOfWork.GetRepository<Course, int>().GetByIdAsync(quiz.CourseId);

            var questionSpec = new QuestionsByQuizSpec(quizId);
            var questions = await _unitOfWork.GetRepository<Question, int>().GetAllAsync(questionSpec);

            var studentQuizSpec = new StudentQuizByStudentAndQuizzesSpec(studentId, new[] { quizId });
            var studentQuizzes = await _unitOfWork.GetRepository<StudentQuiz, int>().GetAllAsync(studentQuizSpec);
            var submission = studentQuizzes.FirstOrDefault();

            var questionDtos = questions.Select(q =>
            {
                var options = string.IsNullOrEmpty(q.Options)
                    ? new List<string>()
                    : q.Options.Split(',').Select(o => o.Trim()).ToList();

                return new QuestionDto
                {
                    Id = q.Id,
                    Type = q.Type.ToString().ToLower(),
                    Prompt = q.Prompt,
                    Options = options,
                    Points = q.Points,
                    CorrectAnswer = q.CorrectAnswer
                };
            }).ToList();

            var summary = new QuestionSummaryDto
            {
                Total = questions.Count(),
                Tf = questions.Count(q => q.Type == QuestionType.TF),
                Mcq = questions.Count(q => q.Type == QuestionType.MCQ),
                Written = questions.Count(q => q.Type == QuestionType.Written)
            };

            return new QuizStartDto
            {
                CourseId = course?.CourseId.ToString() ?? "",
                CourseName = course?.CourseName ?? "",
                Title = quiz.Title,
                DurationSeconds = quiz.DurationSeconds,
                PageSize = quiz.PageSize,
                MaxAttempts = quiz.MaxAttempts,
                Questions = questionDtos,
                QuestionSummary = summary,
                PreviousSubmission = null,
                IsSubmitted = submission?.SubmittedAt != null
            };
        }
    }

    internal class StudentAnswerJson
    {
        public int QuestionId { get; set; }
        public string Answer { get; set; }
    }
}
