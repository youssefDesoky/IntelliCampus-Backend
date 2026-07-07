using IntelliCampus.Service_Abstraction.Exceptions;

namespace IntelliCampus.Service.Exceptions;

public sealed class QuizNotFoundException(int id) : NotFoundException($"Quiz With Id {id} Is Not Found")
    {
    }

    public sealed class CourseNotFoundException : NotFoundException
    {
        public CourseNotFoundException(int id) : base($"Course With Id {id} Is Not Found") { }
        public CourseNotFoundException(string code) : base($"Course With Code {code} Is Not Found") { }
        public CourseNotFoundException() : base("Course not found.") { }
    }

    public sealed class QuestionNotFoundException(int id) : NotFoundException($"Question With Id {id} Is Not Found")
    {
    }

    public sealed class SubmissionNotFoundException(int studentId, int quizId)
        : NotFoundException($"Submission for student {studentId} and quiz {quizId} Is Not Found")
    {
    }

    public sealed class AssignmentNotFoundException : NotFoundException
    {
        public AssignmentNotFoundException(int id) : base($"Assignment With Id {id} Is Not Found") { }
        public AssignmentNotFoundException(string message) : base(message) { }
    }

    public sealed class BylawNotFoundException : NotFoundException
    {
        public BylawNotFoundException(int id) : base($"Bylaw With Id {id} Is Not Found") { }
        public BylawNotFoundException(string message) : base(message) { }
    }

    public sealed class DepartmentNotFoundException : NotFoundException
    {
        public DepartmentNotFoundException(int id) : base($"Department With Id {id} Is Not Found") { }
        public DepartmentNotFoundException(string message) : base(message) { }
    }

    public sealed class ClassNotFoundException : NotFoundException
    {
        public ClassNotFoundException(int id) : base($"Class With Id {id} Is Not Found") { }
        public ClassNotFoundException(string message) : base(message) { }
    }

    public sealed class SessionNotFoundException : NotFoundException
    {
        public SessionNotFoundException(int id) : base($"Session With Id {id} Is Not Found") { }
        public SessionNotFoundException(string message) : base(message) { }
    }

    public sealed class ExamNotFoundException : NotFoundException
    {
        public ExamNotFoundException(int id) : base($"Exam With Id {id} Is Not Found") { }
        public ExamNotFoundException(string message) : base(message) { }
    }

    public sealed class GradeNotFoundException : NotFoundException
    {
        public GradeNotFoundException(int id) : base($"Grade With Id {id} Is Not Found") { }
        public GradeNotFoundException(string message) : base(message) { }
    }

    public sealed class RoomNotFoundException : NotFoundException
    {
        public RoomNotFoundException(int id) : base($"Room With Id {id} Is Not Found") { }
        public RoomNotFoundException(string message) : base(message) { }
    }

    public sealed class MeetingNotFoundException : NotFoundException
    {
        public MeetingNotFoundException(int id) : base($"Meeting With Id {id} Is Not Found") { }
        public MeetingNotFoundException(string message) : base(message) { }
    }

    public sealed class MaterialNotFoundException : NotFoundException
    {
        public MaterialNotFoundException(int id) : base($"Material With Id {id} Is Not Found") { }
        public MaterialNotFoundException() : base("Material not found.") { }
    }

    public sealed class FolderNotFoundException : NotFoundException
    {
        public FolderNotFoundException(int id) : base($"Folder With Id {id} Is Not Found") { }
        public FolderNotFoundException(string message) : base(message) { }
        public FolderNotFoundException() : base("Folder not found.") { }
    }

    public sealed class GroupNotFoundException : NotFoundException
    {
        public GroupNotFoundException(int id) : base($"Group With Id {id} Is Not Found") { }
        public GroupNotFoundException() : base("Group not found.") { }
    }

    public sealed class NotificationNotFoundException : NotFoundException
    {
        public NotificationNotFoundException(int id) : base($"Notification With Id {id} Is Not Found") { }
        public NotificationNotFoundException(string message) : base(message) { }
    }

    public sealed class FriendRequestNotFoundException : NotFoundException
    {
        public FriendRequestNotFoundException(int id) : base($"Friend request With Id {id} Is Not Found") { }
        public FriendRequestNotFoundException(string message) : base(message) { }
    }

    public sealed class FriendshipNotFoundException : NotFoundException
    {
        public FriendshipNotFoundException(string message) : base(message) { }
    }

    public sealed class ChatMessageNotFoundException : NotFoundException
    {
        public ChatMessageNotFoundException(int id) : base($"Message With Id {id} Is Not Found") { }
        public ChatMessageNotFoundException(string message) : base(message) { }
    }

    public sealed class PostNotFoundException : NotFoundException
    {
        public PostNotFoundException(int id) : base($"Post With Id {id} Is Not Found") { }
        public PostNotFoundException(string message) : base(message) { }
    }

    public sealed class CommentNotFoundException : NotFoundException
    {
        public CommentNotFoundException(int id) : base($"Comment With Id {id} Is Not Found") { }
        public CommentNotFoundException(string message) : base(message) { }
    }

    public sealed class ExcuseNotFoundException : NotFoundException
    {
        public ExcuseNotFoundException(int id) : base($"Excuse With Id {id} Is Not Found") { }
        public ExcuseNotFoundException(string message) : base(message) { }
    }

    public sealed class BylawCourseNotFoundException : NotFoundException
    {
        public BylawCourseNotFoundException(int id) : base($"BylawCourse With Id {id} Is Not Found") { }
        public BylawCourseNotFoundException(string message) : base(message) { }
    }

    public sealed class ComplaintNotFoundException : NotFoundException
    {
        public ComplaintNotFoundException(int id) : base($"Complaint With Id {id} Is Not Found") { }
        public ComplaintNotFoundException(string message) : base(message) { }
    }

    public sealed class ScheduleNotFoundException : NotFoundException
    {
        public ScheduleNotFoundException(int id) : base($"Schedule With Id {id} Is Not Found") { }
        public ScheduleNotFoundException(string message) : base(message) { }
    }

    public sealed class ExamScheduleNotFoundException : NotFoundException
    {
        public ExamScheduleNotFoundException(int id) : base($"ExamSchedule With Id {id} Is Not Found") { }
        public ExamScheduleNotFoundException(string message) : base(message) { }
    }

    public sealed class AnnouncementNotFoundException : NotFoundException
    {
        public AnnouncementNotFoundException(int id) : base($"Announcement With Id {id} Is Not Found") { }
        public AnnouncementNotFoundException(string message) : base(message) { }
    }

    public sealed class ElectiveBucketNotFoundException : NotFoundException
    {
        public ElectiveBucketNotFoundException(int id) : base($"ElectiveBucket With Id {id} Is Not Found") { }
        public ElectiveBucketNotFoundException(string message) : base(message) { }
    }

    public sealed class AdminNotFoundException : NotFoundException
    {
        public AdminNotFoundException(int id) : base($"Admin With Id {id} Is Not Found") { }
        public AdminNotFoundException(string message) : base(message) { }
    }

    public sealed class UserNotFoundException : NotFoundException
    {
        public UserNotFoundException(int id) : base($"User With Id {id} Is Not Found") { }
        public UserNotFoundException(string message) : base(message) { }
    }

    public sealed class StudentAssignmentNotFoundException : NotFoundException
    {
        public StudentAssignmentNotFoundException(int id) : base($"StudentAssignment With Id {id} Is Not Found") { }
        public StudentAssignmentNotFoundException(string message) : base(message) { }
    }

    public sealed class RoleNotFoundException : NotFoundException
    {
        public RoleNotFoundException(int id) : base($"Role With Id {id} Is Not Found") { }
        public RoleNotFoundException(string message) : base(message) { }
    }

    public sealed class ReminderNotFoundException : NotFoundException
    {
        public ReminderNotFoundException(int id) : base($"Reminder With Id {id} Is Not Found") { }
        public ReminderNotFoundException(string message) : base(message) { }
    }

    public sealed class StudentNotFoundException : NotFoundException
    {
        public StudentNotFoundException(int id) : base($"Student With Id {id} Is Not Found") { }
        public StudentNotFoundException(string message) : base(message) { }
    }

    public sealed class InstructorNotFoundException : NotFoundException
    {
        public InstructorNotFoundException(int id) : base($"Instructor With Id {id} Is Not Found") { }
        public InstructorNotFoundException(string message) : base(message) { }
    }



    public sealed class InternalMessageNotFoundException : NotFoundException
    {
        public InternalMessageNotFoundException(int id) : base($"Internal message With Id {id} Is Not Found") { }
        public InternalMessageNotFoundException(string message) : base(message) { }
    }

public sealed class NoteNotFoundException : NotFoundException
{
    public NoteNotFoundException(int id) : base($"Note With Id {id} Is Not Found") { }
    public NoteNotFoundException(string message) : base(message) { }
}

public sealed class BroadcastAnnouncementNotFoundException : NotFoundException
{
    public BroadcastAnnouncementNotFoundException(int id) : base($"Broadcast announcement With Id {id} Is Not Found") { }
    public BroadcastAnnouncementNotFoundException(string message) : base(message) { }
}

public sealed class RegistrationNotFoundException : NotFoundException
{
    public RegistrationNotFoundException(int courseId) : base($"Registration not found for course {courseId}.") { }
}
