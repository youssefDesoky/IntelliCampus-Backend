using IntelliCampus.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

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
    public DbSet<LoanInstructor> LoanInstructors { get; set; }
    public DbSet<Student> Students { get; set; }
    public DbSet<Faculty> Faculties { get; set; }

    // Academic entities
    public DbSet<Course> Courses { get; set; }
    public DbSet<Class> Classes { get; set; }
    public DbSet<Session> Sessions { get; set; }
    public DbSet<Department> Departments { get; set; }
    public DbSet<Room> Rooms { get; set; }
    public DbSet<DepartmentPreference> DepartmentPreferences { get; set; }
    public DbSet<Grade> Grades { get; set; }
    public DbSet<CourseWorkWeight> CourseWorkWeights { get; set; }
    public DbSet<GradeComplaint> GradeComplaints { get; set; }
    public DbSet<Exam> Exams { get; set; }
    public DbSet<ExamSeatAssignment> ExamSeatAssignments { get; set; }

    // Learning materials
    public DbSet<Material> Materials { get; set; }
    public DbSet<MaterialFolder> MaterialFolders { get; set; }
    public DbSet<Assignment> Assignments { get; set; }
    public DbSet<AssignmentAttachment> AssignmentAttachments { get; set; }
    public DbSet<Quiz> Quizzes { get; set; }
    public DbSet<Question> Questions { get; set; }

    // Student activities
    public DbSet<AttendanceExcuse> AttendanceExcuses { get; set; }
    public DbSet<Attendance> Attendances { get; set; }
    public DbSet<Note> Notes { get; set; }
    public DbSet<NoteSummary> NoteSummaries { get; set; }
    public DbSet<Reminder> Reminders { get; set; }
    public DbSet<Schedule> Schedules { get; set; }
    public DbSet<ExamSchedule> ExamSchedules { get; set; }
    public DbSet<ChatbotQuery> ChatbotQueries { get; set; }

    // Community
    public DbSet<Community> Communities { get; set; }
    public DbSet<Post> Posts { get; set; }
    public DbSet<Comment> Comments { get; set; }
    public DbSet<PostVote> PostVotes { get; set; }
    public DbSet<PostCandidate> PostCandidates { get; set; }

    // Notifications
    public DbSet<Notification> Notifications { get; set; }
    public DbSet<DeviceToken> DeviceTokens { get; set; }
    public DbSet<UserNotificationSettings> UserNotificationSettings { get; set; }

    // Announcements
    public DbSet<Announcement> Announcements { get; set; }
    public DbSet<AnnouncementAttachment> AnnouncementAttachments { get; set; }
    public DbSet<AnnouncementComment> AnnouncementComments { get; set; }

    // Broadcast announcements
    public DbSet<BroadcastAnnouncement> BroadcastAnnouncements { get; set; }

    // Attendance
    public DbSet<QrToken> QrTokens { get; set; }

    // Bylaw
    public DbSet<Bylaw> Bylaws { get; set; }
    public DbSet<BylawCourse> BylawCourses { get; set; }
    public DbSet<BylawCoursePrerequisite> BylawCoursePrerequisites { get; set; }

    // Junction tables
    public DbSet<StudentCourse> StudentCourses { get; set; }
    public DbSet<StudentQuiz> StudentQuizzes { get; set; }
    public DbSet<StudentAssignment> StudentAssignments { get; set; }
    public DbSet<SubmissionFile> SubmissionFiles { get; set; }
    public DbSet<StudentDepartment> StudentDepartments { get; set; }
    public DbSet<InstructorMaterial> InstructorMaterials { get; set; }
    public DbSet<UserNotification> UserNotifications { get; set; }
    public DbSet<CoursePrerequisite> CoursePrerequisites { get; set; }

    // Chat
    public DbSet<ChatMessage> ChatMessages { get; set; }

    // Friends
    public DbSet<FriendRequest> FriendRequests { get; set; }
    public DbSet<Friendship> Friendships { get; set; }

    // Meetings
    public DbSet<Meeting> Meetings { get; set; }

    // Groups
    public DbSet<Group> Groups { get; set; }
    public DbSet<GroupMember> GroupMembers { get; set; }

    // Roles
    public DbSet<Role> Roles { get; set; }
    public DbSet<UserRoleJunction> UserRoleJunctions { get; set; }

    // Security / Auth
    public DbSet<SecurityAuditLog> SecurityAuditLogs { get; set; }
    public DbSet<EmailVerificationCode> EmailVerificationCodes { get; set; }
    public DbSet<PasswordResetToken> PasswordResetTokens { get; set; }

    // Elective Buckets
    public DbSet<ElectiveBucket> ElectiveBuckets { get; set; }
    public DbSet<ElectiveBucketCourse> ElectiveBucketCourses { get; set; }
    public DbSet<StudentElectiveBucketProgress> StudentElectiveBucketProgresses { get; set; }

    // Inbox
    public DbSet<InternalMessage> InternalMessages { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(IntelliCampusDbContext).Assembly);
    }
}
