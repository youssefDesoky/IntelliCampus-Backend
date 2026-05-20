using System.Collections.Generic;
using IntelliCampus.Domain.Entities.Enums;

namespace IntelliCampus.Domain.Entities
{
    public class Question
    {
        public int Id { get; set; }
        public int QuizId { get; set; }
        public QuestionType Type { get; set; }
        public string Prompt { get; set; }
        public string Options { get; set; }
        public double Points { get; set; }
        public string CorrectAnswer { get; set; }

        public Quiz Quiz { get; set; }
    }
}
