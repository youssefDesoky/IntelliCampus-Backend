using IntelliCampus.Domain.Entities;

namespace IntelliCampus.Service.Specifications;

public class StudentAssignmentSpec : BaseSpecifications<StudentAssignment>
{
    // GetSubmissionAsync — single student + assignment
    public StudentAssignmentSpec(int studentId, int assignmentId)
        : base(sa => sa.StudentId == studentId && sa.AssignmentId == assignmentId)
    {
        AddInclude(sa => sa.Files);
        AddInclude(sa => sa.GradedByInstructor);
        AddInclude(sa => sa.Student);
        AddInclude("Assignment.Class");
    }

    // GetAllSubmissionsAsync — all submissions for an assignment
    public StudentAssignmentSpec(int assignmentId, bool allSubmissions)
        : base(sa => sa.AssignmentId == assignmentId)
    {
        AddInclude(sa => sa.Student);
        AddInclude(sa => sa.Files);
        AddInclude(sa => sa.GradedByInstructor);
        AddInclude(sa => sa.Assignment);
    }

    // GetByStudentIdAsync — all assignments for a student
    public StudentAssignmentSpec(int studentId, bool byStudent, bool dummy)
        : base(sa => sa.StudentId == studentId)
    {
        AddInclude(sa => sa.Assignment);
        AddInclude("Assignment.Attachments");
        AddInclude(sa => sa.Files);
        AddInclude(sa => sa.GradedByInstructor);
        AddOrderByDescending(sa => sa.Assignment.DueDate);
    }
}
