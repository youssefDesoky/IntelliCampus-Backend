namespace IntelliCampus.Domain.Entities;

public class Student : User
{
    public int StudentId { get; set; }
    public string? StudentCode { get; set; }
    public int? Level { get; set; }
    public int? DepartmentId { get; set; }
    public int? BylawId { get; set; }
    public DateTime? EnrollmentDate { get; set; }

    // Navigation properties
    public Department? Department { get; set; }
    public Bylaw? Bylaw { get; set; }
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
    public ICollection<ExamSeatAssignment> ExamSeatAssignments { get; set; } = [];
}
