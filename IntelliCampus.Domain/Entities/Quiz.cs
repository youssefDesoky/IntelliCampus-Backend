using System.Collections.Generic;

namespace IntelliCampus.Domain.Entities
{
    public class Quiz
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public double MaxScore { get; set; }
        public DateTime Deadline { get; set; }
        public int DurationSeconds { get; set; }
        public int PageSize { get; set; }
        public int MaxAttempts { get; set; }

        public int CourseId { get; set; }
        public Course Course { get; set; }

        public ICollection<Question> Questions { get; set; } = new List<Question>();
        public ICollection<StudentQuiz> StudentQuizzes { get; set; } = new List<StudentQuiz>();
    }
}
