using IntelliCampus.Domain.Entities;
using IntelliCampus.Shared.Params;

namespace IntelliCampus.Service.Specifications;

public class SessionSpec : BaseSpecifications<Session>
{
    public SessionSpec(int classId, bool byClass)
        : base(s => s.ClassId == classId)
    {
        AddInclude(s => s.Attendances!);
        AddInclude(s => s.Class!);
        AddInclude($"{nameof(Session.Class)}.{nameof(Class.StudentCourses)}");
        EnableSplitQuery();
        AddOrderByDescending(s => s.Date);
    }

    public SessionSpec(int sessionId)
        : base(s => s.SessionId == sessionId)
    {
        AddInclude(s => s.Attendances!);
        AddInclude(s => s.Class!);
        AddInclude($"{nameof(Session.Class)}.{nameof(Class.StudentCourses)}");
        EnableSplitQuery();
    }

    public SessionSpec(HashSet<int> classIds, SessionQueryParams queryParams)
        : base(s => classIds.Contains(s.ClassId))
    {
        AddInclude(s => s.Attendances!);
        AddInclude(s => s.Class!);
        AddInclude($"{nameof(Session.Class)}.{nameof(Class.StudentCourses)}");
        EnableSplitQuery();
        AddOrderByDescending(s => s.Date);
        ApplyPagination(queryParams.PageSize, queryParams.PageIndex);
    }

    // Batch load sessions by class IDs without pagination, with attendance data
    public SessionSpec(HashSet<int> classIds)
        : base(s => classIds.Contains(s.ClassId))
    {
        AddInclude(s => s.Attendances!);
        AddOrderByDescending(s => s.Date);
    }
}

internal class SessionCountSpec : BaseSpecifications<Session>
{
    public SessionCountSpec(HashSet<int> classIds)
        : base(s => classIds.Contains(s.ClassId))
    {
    }
}
