using IntelliCampus.Domain.Entities;
using IntelliCampus.Domain.Entities.Enums;
using IntelliCampus.Shared.Params;

namespace IntelliCampus.Service.Specifications
{
    internal class StudentSpec : BaseSpecifications<Student>
    {
        public StudentSpec()
            : base(null)
        {
            AddInclude(s => s.User.Faculty!);
            AddInclude(s => s.Department!);
            AddInclude(s => s.Bylaw!);
            AddInclude("User.UserRoles.Role");
            EnableSplitQuery();
        }

        public StudentSpec(StudentQueryParams queryParams)
            : base(StudentSpecHelper.GetStudentCriteria(queryParams))
        {
            AddInclude(s => s.User.Faculty!);
            AddInclude(s => s.Department!);
            AddInclude(s => s.Bylaw!);
            AddInclude("User.UserRoles.Role");
            EnableSplitQuery();
            AddOrderBy(s => s.User.FullName);
            ApplyPagination(queryParams.PageSize, queryParams.PageIndex);
        }

public StudentSpec(CourseQueryParams queryParams)
    : base(queryParams.StudentId.HasValue ? s => s.UserId == queryParams.StudentId.Value : null)
{
    AddInclude(s => s.User.Faculty!);
    AddInclude(s => s.Department!);
    AddInclude(s => s.Bylaw!);
    AddInclude("Bylaw.GradeScales");
    AddInclude("User.UserRoles.Role");

    if (queryParams.IncludeCourses)
    {
        AddInclude("StudentCourses.Course.Notes.MaterialFolder");
        AddInclude("StudentCourses.Course.Notes.NoteSummary");
    }

    EnableSplitQuery();
}

// Lightweight — no includes (for operations needing only scalar properties)
public StudentSpec(CourseQueryParams queryParams, bool lightweight)
    : base(queryParams.StudentId.HasValue ? s => s.UserId == queryParams.StudentId.Value : null) { }

public StudentSpec(List<int> studentIds)
    : base(s => studentIds.Contains(s.UserId))
{
    AddInclude(s => s.User.Faculty!);
    AddInclude(s => s.Department!);
    AddInclude(s => s.Bylaw!);
    AddInclude("User.UserRoles.Role");
    EnableSplitQuery();
}

// Students with Gpa > 0 (no includes needed for aggregate)
public StudentSpec(bool hasGpa)
    : base(s => hasGpa && s.Gpa > 0) { }

    // Students with User + Bylaw include (for probation & faculty-scoping)
    public StudentSpec(bool includeBylaw, bool forProbation)
        : base(null)
    {
        AddInclude("User");
        if (includeBylaw)
        {
            AddInclude(s => s.Department!);
            AddInclude(s => s.Bylaw!);
        }
    }

// Batch load by IDs with User include (used by MapToDtoWithDetails)
public StudentSpec(List<int> ids, bool lightweight)
    : base(s => ids.Contains(s.UserId))
{
    AddInclude(s => s.User!);
}

// Student by student code
public StudentSpec(string studentCode, bool byCode)
    : base(s => s.StudentCode == studentCode)
{
    AddInclude(s => s.User!);
}

// Batch load by student codes (no includes)
public StudentSpec(List<string> studentCodes, bool byCodes)
    : base(s => s.StudentCode != null && studentCodes.Contains(s.StudentCode)) { }
    }
}
