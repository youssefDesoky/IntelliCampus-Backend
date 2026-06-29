using IntelliCampus.Domain.Entities;

namespace IntelliCampus.Service.Specifications;

public class StudentAssignmentSpec : BaseSpecifications<StudentAssignment>
{
    // GetSubmissionAsync � single student + assignment
    public StudentAssignmentSpec(int studentId, int assignmentId)
        : base(sa => sa.StudentId == studentId && sa.AssignmentId == assignmentId)
    {
        AddInclude(sa => sa.Files!);
        AddInclude(sa => sa.GradedByInstructor!);
        AddInclude(sa => sa.Student!);
        AddInclude("Assignment.Course");
        EnableSplitQuery();
    }

    // GetAllSubmissionsAsync � all submissions for an assignment
    public StudentAssignmentSpec(int assignmentId, bool allSubmissions)
        : base(sa => sa.AssignmentId == assignmentId)
    {
        AddInclude(sa => sa.Student!);
        AddInclude(sa => sa.Files!);
        AddInclude(sa => sa.GradedByInstructor!);
        AddInclude(sa => sa.Assignment!);
        EnableSplitQuery();
    }

    // GetByStudentIdAsync � all assignments for a student
    public StudentAssignmentSpec(int studentId, bool byStudent, bool dummy)
        : base(sa => sa.StudentId == studentId)
    {
        AddInclude(sa => sa.Assignment!);
        AddInclude("Assignment.Attachments");
        AddInclude(sa => sa.Files!);
        AddInclude(sa => sa.GradedByInstructor!);
        EnableSplitQuery();
        AddOrderByDescending(sa => sa.Assignment.DueDate);
    }

    // GetByStudentIdForTranscriptAsync — all submissions for a student, no includes
    public StudentAssignmentSpec(int studentId, string scope)
        : base(sa => sa.StudentId == studentId) { }

    // GetByAssignmentIdsAsync — all submissions for a set of assignments
    public StudentAssignmentSpec(ICollection<int> assignmentIds, bool byAssignments)
        : base(sa => assignmentIds.Contains(sa.AssignmentId))
    {
        AddInclude(sa => sa.Files!);
        AddInclude(sa => sa.GradedByInstructor!);
        AddInclude(sa => sa.Student!);
        AddInclude(sa => sa.Assignment!);
        EnableSplitQuery();
    }

    // GetByStudentAndAssignmentIdsAsync — submissions for a specific student + set of assignments
    public StudentAssignmentSpec(int studentId, ICollection<int> assignmentIds)
        : base(sa => sa.StudentId == studentId && assignmentIds.Contains(sa.AssignmentId))
    {
        AddInclude(sa => sa.Files!);
        AddInclude(sa => sa.GradedByInstructor!);
        AddInclude(sa => sa.Assignment!);
        AddInclude(sa => sa.Student!);
        EnableSplitQuery();
    }

    // GetBySubmissionIdsAsync — load submissions by their PKs, no includes
    public StudentAssignmentSpec(List<int> submissionIds, string discriminator)
        : base(sa => submissionIds.Contains(sa.StudentAssignmentId)) { }
}
