using System;
using System.Collections.Generic;

namespace IntelliCampus.Domain.Entities;

public class Quiz
{
    public int QuizId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime DueDate { get; set; }
    public int DurationMinutes { get; set; }
    public decimal MaxGrade { get; set; }
    public int TotalMarks { get; set; }
    public int CourseId { get; set; }

    // Navigation properties
    public Course Course { get; set; } = null!;
    public ICollection<Question> Questions { get; set; } = new List<Question>();
    public ICollection<StudentQuiz> StudentQuizzes { get; set; } = new List<StudentQuiz>();
}
