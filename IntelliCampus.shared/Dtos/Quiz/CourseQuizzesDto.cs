using System.Collections.Generic;

namespace IntelliCampus.Shared.Dtos.Quiz
{
    public class CourseQuizzesDto
    {
        public string CourseId { get; set; }
        public string CourseName { get; set; }
        public List<QuizDto> History { get; set; }
        public List<QuizDto> Upcoming { get; set; }
        public QuizStatsDto Stats { get; set; }
    }
}
