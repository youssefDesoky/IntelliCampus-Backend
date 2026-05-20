using System;
using System.Collections.Generic;

namespace IntelliCampus.Shared.Dtos.Quiz
{
    public class QuizSubmissionResultDto
    {
        public int StudentId { get; set; }
        public int QuizId { get; set; }
        public string QuizTitle { get; set; }
        public double TotalScore { get; set; }
        public double MaxScore { get; set; }
        public double AutoGradedScore { get; set; }
        public double ManualScore { get; set; }
        public DateTime? GradedAt { get; set; }
        public List<QuestionResultDto> QuestionResults { get; set; }
    }

    public class QuestionResultDto
    {
        public int QuestionId { get; set; }
        public string Type { get; set; }
        public string Prompt { get; set; }
        public string StudentAnswer { get; set; }
        public string CorrectAnswer { get; set; }
        public bool IsCorrect { get; set; }
        public double Points { get; set; }
        public double Earned { get; set; }
        public string? Feedback { get; set; }
    }
}
