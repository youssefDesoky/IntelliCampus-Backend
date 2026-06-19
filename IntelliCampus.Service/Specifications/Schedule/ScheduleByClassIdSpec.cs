using IntelliCampus.Domain.Entities;

namespace IntelliCampus.Service.Specifications;

public sealed class ScheduleByClassIdSpec : BaseSpecifications<Schedule>
{
    public ScheduleByClassIdSpec(int classId)
        : base(s => s.ClassId == classId)
    {
        AddOrderBy(s => s.Date);
    }
}
