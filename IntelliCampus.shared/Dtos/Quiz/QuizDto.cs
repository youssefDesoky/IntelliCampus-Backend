using System;

namespace IntelliCampus.Shared.Dtos.Quiz
{
    public class QuizDto
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public double? Score { get; set; }
        public double MaxScore { get; set; }
        public DateTime Deadline { get; set; }
        public string Status { get; set; }
    }
}
