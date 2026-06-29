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
            AddInclude(s => s.Faculty!);
            AddInclude(s => s.Department!);
            AddInclude(s => s.Bylaw!);
            AddInclude(s => s.Specialization!);
            AddInclude("UserRoles.Role");
            EnableSplitQuery();
        }

        public StudentSpec(StudentQueryParams queryParams)
            : base(StudentSpecHelper.GetStudentCriteria(queryParams))
        {
            AddInclude(s => s.Faculty!);
            AddInclude(s => s.Department!);
            AddInclude(s => s.Bylaw!);
            AddInclude(s => s.Specialization!);
            AddInclude("UserRoles.Role");
            EnableSplitQuery();
            AddOrderBy(s => s.FullName);
            ApplyPagination(queryParams.PageSize, queryParams.PageIndex);
        }

public StudentSpec(CourseQueryParams queryParams)
    : base(queryParams.StudentId.HasValue ? s => s.UserId == queryParams.StudentId.Value : null)
{
    AddInclude(s => s.Faculty!);
    AddInclude(s => s.Department!);
    AddInclude(s => s.Bylaw!);
    AddInclude(s => s.Specialization!);
    AddInclude("UserRoles.Role");

    if (queryParams.IncludeCourses)
    {
        AddInclude("StudentCourses.Course.Notes.MaterialFolder");
    }

    EnableSplitQuery();
}

// Lightweight — no includes (for operations needing only scalar properties)
public StudentSpec(CourseQueryParams queryParams, bool lightweight)
    : base(queryParams.StudentId.HasValue ? s => s.UserId == queryParams.StudentId.Value : null) { }

public StudentSpec(List<int> studentIds)
    : base(s => studentIds.Contains(s.UserId))
{
    AddInclude(s => s.Faculty!);
    AddInclude(s => s.Department!);
    AddInclude(s => s.Bylaw!);
    AddInclude(s => s.Specialization!);
    AddInclude("UserRoles.Role");
    EnableSplitQuery();
}

// Students with Gpa > 0 (no includes needed for aggregate)
public StudentSpec(bool hasGpa)
    : base(s => hasGpa && s.Gpa > 0) { }

// Batch load by IDs with no includes (lightweight)
public StudentSpec(List<int> ids, bool lightweight)
    : base(s => ids.Contains(s.UserId)) { }

// Student by student code (no includes)
public StudentSpec(string studentCode, bool byCode)
    : base(s => s.StudentCode == studentCode) { }

// Batch load by student codes (no includes)
public StudentSpec(List<string> studentCodes, bool byCodes)
    : base(s => s.StudentCode != null && studentCodes.Contains(s.StudentCode)) { }
    }
}
