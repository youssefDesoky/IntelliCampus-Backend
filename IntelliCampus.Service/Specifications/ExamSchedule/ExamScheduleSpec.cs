using IntelliCampus.Domain.Entities;
using IntelliCampus.Domain.Entities.Enums;
using IntelliCampus.Shared.Params;

namespace IntelliCampus.Service.Specifications;

public class ExamScheduleSpec : BaseSpecifications<ExamSchedule>
{
    public ExamScheduleSpec(int studentId)
        : base(e => e.StudentId == studentId)
    {
        AddInclude(e => e.Course!);
        AddInclude(e => e.Room!);
        AddInclude(e => e.Exam!);
        AddInclude("Exam.ExamSeatAssignments");
        AddInclude("Exam.ExamSeatAssignments.Room");
        EnableSplitQuery();
        AddOrderBy(e => e.Date);
    }

    public ExamScheduleSpec(int studentId, ExamType examType)
        : base(e => e.StudentId == studentId && e.ExamType == examType)
    {
        AddInclude(e => e.Course!);
        AddInclude(e => e.Room!);
        AddInclude(e => e.Exam!);
        AddInclude("Exam.ExamSeatAssignments");
        AddInclude("Exam.ExamSeatAssignments.Room");
        EnableSplitQuery();
        AddOrderBy(e => e.Date);
    }

    public ExamScheduleSpec(int studentId, ExamStatus status)
        : base(e => e.StudentId == studentId && e.Status == status)
    {
        AddInclude(e => e.Course!);
        AddInclude(e => e.Room!);
        AddInclude(e => e.Exam!);
        AddInclude("Exam.ExamSeatAssignments");
        AddInclude("Exam.ExamSeatAssignments.Room");
        EnableSplitQuery();
        AddOrderBy(e => e.Date);
    }

    public ExamScheduleSpec(int examScheduleId, bool byId)
        : base(e => e.ExamScheduleId == examScheduleId)
    {
        AddInclude(e => e.Course!);
        AddInclude(e => e.Room!);
        AddInclude(e => e.Exam!);
        AddInclude("Exam.ExamSeatAssignments");
        AddInclude("Exam.ExamSeatAssignments.Room");
        EnableSplitQuery();
    }

    public ExamScheduleSpec(int studentId, ExamType examType, ExamScheduleQueryParams queryParams)
        : base(e => e.StudentId == studentId && e.ExamType == examType)
    {
        AddInclude(e => e.Course!);
        AddInclude(e => e.Room!);
        AddInclude(e => e.Exam!);
        AddInclude("Exam.ExamSeatAssignments");
        AddInclude("Exam.ExamSeatAssignments.Room");
        EnableSplitQuery();
        AddOrderBy(e => e.Date);
        ApplyPagination(queryParams.PageSize, queryParams.PageIndex);
    }

    public ExamScheduleSpec(int studentId, ExamStatus status, ExamScheduleQueryParams queryParams)
        : base(e => e.StudentId == studentId && e.Status == status)
    {
        AddInclude(e => e.Course!);
        AddInclude(e => e.Room!);
        AddInclude(e => e.Exam!);
        AddInclude("Exam.ExamSeatAssignments");
        AddInclude("Exam.ExamSeatAssignments.Room");
        EnableSplitQuery();
        AddOrderBy(e => e.Date);
        ApplyPagination(queryParams.PageSize, queryParams.PageIndex);
    }

    public ExamScheduleSpec(int studentId, int pageSize, int pageIndex)
        : base(e => e.StudentId == studentId)
    {
        AddInclude(e => e.Course!);
        AddInclude(e => e.Room!);
        AddInclude(e => e.Exam!);
        AddInclude("Exam.ExamSeatAssignments");
        AddInclude("Exam.ExamSeatAssignments.Room");
        EnableSplitQuery();
        AddOrderBy(e => e.Date);
        ApplyPagination(pageSize, pageIndex);
    }

    public ExamScheduleSpec(int studentId, ExamScheduleQueryParams queryParams, bool forCount = false)
        : base(e => e.StudentId == studentId
            && (!queryParams.Type.HasValue || e.ExamType == queryParams.Type.Value)
            && (!queryParams.Status.HasValue || e.Status == queryParams.Status.Value))
    {
        AddInclude(e => e.Course!);
        AddInclude(e => e.Room!);
        AddInclude(e => e.Exam!);
        AddInclude("Exam.ExamSeatAssignments");
        AddInclude("Exam.ExamSeatAssignments.Room");
        EnableSplitQuery();
        AddOrderBy(e => e.Date);
        if (!forCount)
            ApplyPagination(queryParams.PageSize, queryParams.PageIndex);
    }
}
