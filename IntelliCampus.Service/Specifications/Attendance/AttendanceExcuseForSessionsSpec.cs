using IntelliCampus.Domain.Entities;

namespace IntelliCampus.Service.Specifications;

public class AttendanceExcuseForSessionsSpec : BaseSpecifications<AttendanceExcuse>
{
    public AttendanceExcuseForSessionsSpec(HashSet<int> sessionIds)
        : base(e => sessionIds.Contains(e.SessionId))
    {
        AddInclude(e => e.Student!);
        AddOrderByDescending(e => e.CreatedAt);
    }
}
