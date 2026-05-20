using System;

namespace IntelliCampus.Domain.Entities
{
    public class StudentQuiz
    {
        public int StudentId { get; set; }
        public int QuizId { get; set; }
        public decimal? Score { get; set; }
        public DateTime StartedAt { get; set; }
        public DateTime? SubmittedAt { get; set; }
        public int AttemptNumber { get; set; }
        public string Answers { get; set; }
        public int? GradedByInstructorId { get; set; }
        public DateTime? GradedAt { get; set; }
        public string? Feedback { get; set; }

        // Navigation properties
        public Student Student { get; set; }
        public Quiz Quiz { get; set; }
        public Instructor? GradedByInstructor { get; set; }
    }
}
