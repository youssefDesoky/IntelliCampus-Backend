using System.Collections.Generic;

namespace IntelliCampus.Shared.Dtos.Quiz
{
    public class GradeQuizSubmissionDto
    {
        public int StudentId { get; set; }
        public int QuizId { get; set; }
        public List<WrittenQuestionGradeDto> WrittenScores { get; set; }
    }

    public class WrittenQuestionGradeDto
    {
        public int QuestionId { get; set; }
        public double Score { get; set; }
        public string? Feedback { get; set; }
    }
}
