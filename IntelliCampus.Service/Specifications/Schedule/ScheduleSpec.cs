using IntelliCampus.Domain.Entities;
using IntelliCampus.Domain.Entities.Enums;
using IntelliCampus.Shared.Params;

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

    // Weekly entries filtered by ScheduleQueryParams
    public ScheduleSpec(int studentId, ScheduleType type, ScheduleQueryParams queryParams)
        : base(s => s.StudentId == studentId && s.ScheduleType == type)
    {
        AddInclude(s => s.Course!);
        AddOrderBy(s => s.Date);
        ApplyPagination(queryParams.PageSize, queryParams.PageIndex);
    }

    public ScheduleSpec(int studentId, int pageSize, int pageIndex)
        : base(s => s.StudentId == studentId)
    {
        AddInclude(s => s.Course!);
        AddOrderBy(s => s.Date);
        ApplyPagination(pageSize, pageIndex);
    }

    public ScheduleSpec(int studentId, ScheduleQueryParams queryParams, bool forCount = false)
        : base(s => s.StudentId == studentId
            && (queryParams.Types == null || queryParams.Types.Length == 0
                || queryParams.Types.Contains(s.ScheduleType)))
    {
        AddInclude(s => s.Course!);
        AddOrderBy(s => s.Date);
        if (!forCount)
            ApplyPagination(queryParams.PageSize, queryParams.PageIndex);
    }

    // Single entry by id
    public ScheduleSpec(int scheduleId, bool byId)
        : base(s => s.ScheduleId == scheduleId)
    {
        AddInclude(s => s.Course!);
    }
}
