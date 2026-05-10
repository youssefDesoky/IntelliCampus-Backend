using IntelliCampus.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace IntelliCampus.Presistence.Data.Contexts;

public class IntelliCampusDbContext : DbContext
{
    public IntelliCampusDbContext(DbContextOptions<IntelliCampusDbContext> options) : base(options)
    {
    }

    // Core entities
    public DbSet<User> Users { get; set; }
    public DbSet<Admin> Admins { get; set; }
    public DbSet<Instructor> Instructors { get; set; }
    public DbSet<Student> Students { get; set; }

    // Academic entities
    public DbSet<Course> Courses { get; set; }
    public DbSet<Class> Classes { get; set; }
    public DbSet<Session> Sessions { get; set; }
    public DbSet<Department> Departments { get; set; }
    public DbSet<Grade> Grades { get; set; }
    public DbSet<Exam> Exams { get; set; }

    // Learning materials
    public DbSet<Material> Materials { get; set; }
    public DbSet<MaterialFolder> MaterialFolders { get; set; }
    public DbSet<Assignment> Assignments { get; set; }
    public DbSet<Quiz> Quizzes { get; set; }

    // Student activities
    public DbSet<Attendance> Attendances { get; set; }
    public DbSet<Note> Notes { get; set; }
    public DbSet<NoteSummary> NoteSummaries { get; set; }
    public DbSet<Reminder> Reminders { get; set; }
    public DbSet<Schedule> Schedules { get; set; }
    public DbSet<ChatbotQuery> ChatbotQueries { get; set; }

    // Community
    public DbSet<Community> Communities { get; set; }
    public DbSet<Post> Posts { get; set; }
    public DbSet<Comment> Comments { get; set; }

    // Notifications
    public DbSet<Notification> Notifications { get; set; }

    // Junction tables
    public DbSet<StudentCourse> StudentCourses { get; set; }
    public DbSet<StudentQuiz> StudentQuizzes { get; set; }
    public DbSet<StudentAssignment> StudentAssignments { get; set; }
    public DbSet<StudentDepartment> StudentDepartments { get; set; }
    public DbSet<InstructorMaterial> InstructorMaterials { get; set; }
    public DbSet<UserNotification> UserNotifications { get; set; }
    public DbSet<CoursePrerequisite> CoursePrerequisites { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(IntelliCampusDbContext).Assembly);
    }
}
