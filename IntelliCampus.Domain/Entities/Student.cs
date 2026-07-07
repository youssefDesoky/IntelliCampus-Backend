using IntelliCampus.Domain.Entities.Enums;

namespace IntelliCampus.Domain.Entities;

public class Student
{
    public int UserId { get; set; }
    public string? StudentCode { get; set; }
    public int? Level { get; set; }
    public int? DepartmentId { get; set; }
    public int? BylawId { get; set; }
    public DateTime? EnrollmentDate { get; set; }
    public StudentProgram? Program { get; set; }
    public double Gpa { get; set; }
    public Enums.StudentType StudentType { get; set; }

    // Navigation properties
    public User User { get; set; } = null!;
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
    public ICollection<StudentElectiveBucketProgress> ElectiveBucketProgresses { get; set; } = [];
    public ICollection<DepartmentPreference> DepartmentPreferences { get; set; } = [];
}
