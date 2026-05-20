namespace IntelliCampus.Shared.Dtos.Quiz
{
    public class QuizStartDto
    {
        public string CourseId { get; set; }
        public string CourseName { get; set; }
        public string Title { get; set; }
        public int DurationSeconds { get; set; }
        public int PageSize { get; set; }
        public int MaxAttempts { get; set; }
        public List<QuestionDto> Questions { get; set; }
        public QuestionSummaryDto QuestionSummary { get; set; }
        public object PreviousSubmission { get; set; }
        public bool IsSubmitted { get; set; }
    }
}
