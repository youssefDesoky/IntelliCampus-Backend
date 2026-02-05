namespace IntelliCampus.DAL.Entities;

public class Student : User
{
    public int StudentId { get; set; }
    public string? Faculty { get; set; }
    public int? Level { get; set; }

    // Navigation properties
    public ICollection<StudentQuiz> StudentQuizzes { get; set; } = [];
    public ICollection<StudentAssignment> StudentAssignments { get; set; } = [];
    public ICollection<ChatbotQuery> ChatbotQueries { get; set; } = [];
    public ICollection<Reminder> Reminders { get; set; } = [];
    public ICollection<Note> Notes { get; set; } = [];
    public ICollection<Attendance> Attendances { get; set; } = [];
    public ICollection<Schedule> Schedules { get; set; } = [];
    public ICollection<StudentDepartment> StudentDepartments { get; set; } = [];
    public ICollection<Grade> Grades { get; set; } = [];
    public ICollection<StudentCourse> StudentCourses { get; set; } = [];
}
