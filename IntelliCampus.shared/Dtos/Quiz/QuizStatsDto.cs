namespace IntelliCampus.Shared.Dtos.Quiz
{
    public class QuizStatsDto
    {
        public int Completed { get; set; }
        public int Missed { get; set; }
        public int Upcoming { get; set; }
        public double AverageScore { get; set; }
    }
}
