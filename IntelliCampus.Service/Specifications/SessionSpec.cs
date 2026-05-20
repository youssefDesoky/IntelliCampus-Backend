using IntelliCampus.Domain.Entities;

namespace IntelliCampus.Service.Specifications;

public class SessionSpec : BaseSpecifications<Session>
{
    public SessionSpec(int classId, bool byClass)
        : base(s => s.ClassId == classId)
    {
        AddInclude(s => s.Attendances);
        AddInclude(s => s.Class);
        AddOrderByDescending(s => s.Date);
    }

    public SessionSpec(int sessionId)
        : base(s => s.SessionId == sessionId)
    {
        AddInclude(s => s.Attendances);
        AddInclude(s => s.Class);
    }
}
