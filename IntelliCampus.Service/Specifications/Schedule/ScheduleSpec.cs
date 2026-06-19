using IntelliCampus.Domain.Entities;
using IntelliCampus.Domain.Entities.Enums;

namespace IntelliCampus.Service.Specifications;

public class ScheduleSpec : BaseSpecifications<Schedule>
{
    // All weekly entries for a student
    public ScheduleSpec(int studentId)
        : base(s => s.StudentId == studentId)
    {
        AddInclude(s => s.Course!);
        AddOrderBy(s => s.Date);
    }

    // Weekly entries filtered by ScheduleType
    public ScheduleSpec(int studentId, ScheduleType type)
        : base(s => s.StudentId == studentId && s.ScheduleType == type)
    {
        AddInclude(s => s.Course!);
        AddOrderBy(s => s.Date);
    }

    // Single entry by id
    public ScheduleSpec(int scheduleId, bool byId)
        : base(s => s.ScheduleId == scheduleId)
    {
        AddInclude(s => s.Course!);
    }
}
