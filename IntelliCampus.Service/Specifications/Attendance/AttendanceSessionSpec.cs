using IntelliCampus.Domain.Entities;

namespace IntelliCampus.Service.Specifications;

internal class AttendanceSessionSpec : BaseSpecifications<Attendance>
{
    public AttendanceSessionSpec(HashSet<int> sessionIds)
        : base(a => sessionIds.Contains(a.SessionId))
    {
    }
}
