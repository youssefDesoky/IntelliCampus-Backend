using IntelliCampus.Domain.Entities;
using IntelliCampus.Shared.Params;

namespace IntelliCampus.Service.Specifications;

public class SessionSpec : BaseSpecifications<Session>
{
    public SessionSpec(int classId, bool byClass)
        : base(s => s.ClassId == classId)
    {
        AddInclude(s => s.Attendances!);
        AddInclude("Attendances.Student");
        AddInclude(s => s.Class!);
        AddOrderByDescending(s => s.Date);
    }

    public SessionSpec(int sessionId)
        : base(s => s.SessionId == sessionId)
    {
        AddInclude(s => s.Attendances!);
        AddInclude("Attendances.Student");
        AddInclude(s => s.Class!);
    }

    public SessionSpec(HashSet<int> classIds, SessionQueryParams queryParams)
        : base(s => classIds.Contains(s.ClassId))
    {
        AddInclude(s => s.Attendances!);
        AddInclude("Attendances.Student");
        AddInclude(s => s.Class!);
        AddOrderByDescending(s => s.Date);
        ApplyPagination(queryParams.PageSize, queryParams.PageIndex);
    }
}

internal class SessionCountSpec : BaseSpecifications<Session>
{
    public SessionCountSpec(HashSet<int> classIds)
        : base(s => classIds.Contains(s.ClassId))
    {
    }
}
