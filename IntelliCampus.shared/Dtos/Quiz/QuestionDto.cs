namespace IntelliCampus.Shared.Dtos.Quiz
{
    public class QuestionDto
    {
        public int Id { get; set; }
        public string Type { get; set; }
        public string Prompt { get; set; }
        public List<string> Options { get; set; }
        public double Points { get; set; }
        public string CorrectAnswer { get; set; }
    }
}
