using IntelliCampus.Domain.Entities;
using IntelliCampus.Domain.Entities.Enums;

namespace IntelliCampus.Service.Specifications;

public class ExamScheduleSpec : BaseSpecifications<ExamSchedule>
{
    // All exam entries for a student
    public ExamScheduleSpec(int studentId)
        : base(e => e.StudentId == studentId)
    {
        AddOrderBy(e => e.Date);
    }

    // Exam entries filtered by ExamType
    public ExamScheduleSpec(int studentId, ExamType examType)
        : base(e => e.StudentId == studentId && e.ExamType == examType)
    {
        AddOrderBy(e => e.Date);
    }

    // Exam entries filtered by ExamStatus
    public ExamScheduleSpec(int studentId, ExamStatus status)
        : base(e => e.StudentId == studentId && e.Status == status)
    {
        AddOrderBy(e => e.Date);
    }

    // Single exam entry by id
    public ExamScheduleSpec(int examScheduleId, bool byId)
        : base(e => e.ExamScheduleId == examScheduleId) { }
}
