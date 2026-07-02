using IntelliCampus.Domain.Entities;
using IntelliCampus.Domain.Entities.Enums;
using IntelliCampus.Shared.Params;

namespace IntelliCampus.Service.Specifications;

public class ScheduleSpec : BaseSpecifications<Schedule>
{
    public ScheduleSpec(int studentId)
        : base(s => s.StudentId == studentId)
    {
        AddInclude(s => s.Course!);
        AddInclude(s => s.Room!);
        AddInclude(s => s.Instructor!);
        AddInclude("Instructor.User");
        AddOrderBy(s => s.Date);
    }

    public ScheduleSpec(int studentId, ScheduleType type)
        : base(s => s.StudentId == studentId && s.ScheduleType == type)
    {
        AddInclude(s => s.Course!);
        AddInclude(s => s.Room!);
        AddInclude(s => s.Instructor!);
        AddInclude("Instructor.User");
        AddOrderBy(s => s.Date);
    }

    public ScheduleSpec(int studentId, ScheduleType type, ScheduleQueryParams queryParams)
        : base(s => s.StudentId == studentId && s.ScheduleType == type)
    {
        AddInclude(s => s.Course!);
        AddInclude(s => s.Room!);
        AddInclude(s => s.Instructor!);
        AddInclude("Instructor.User");
        AddOrderBy(s => s.Date);
        ApplyPagination(queryParams.PageSize, queryParams.PageIndex);
    }

    public ScheduleSpec(int studentId, int pageSize, int pageIndex)
        : base(s => s.StudentId == studentId)
    {
        AddInclude(s => s.Course!);
        AddInclude(s => s.Room!);
        AddInclude(s => s.Instructor!);
        AddInclude("Instructor.User");
        AddOrderBy(s => s.Date);
        ApplyPagination(pageSize, pageIndex);
    }

    public ScheduleSpec(int studentId, ScheduleQueryParams queryParams, bool forCount = false)
        : base(s => s.StudentId == studentId
            && (queryParams.Types == null || queryParams.Types.Length == 0
                || queryParams.Types.Contains(s.ScheduleType)))
    {
        AddInclude(s => s.Course!);
        AddInclude(s => s.Room!);
        AddInclude(s => s.Instructor!);
        AddInclude("Instructor.User");
        AddOrderBy(s => s.Date);
        if (!forCount)
            ApplyPagination(queryParams.PageSize, queryParams.PageIndex);
    }

    public ScheduleSpec(int scheduleId, bool byId)
        : base(s => s.ScheduleId == scheduleId)
    {
        AddInclude(s => s.Course!);
        AddInclude(s => s.Room!);
        AddInclude(s => s.Instructor!);
        AddInclude("Instructor.User");
    }
}
