using IntelliCampus.Domain.Entities;

namespace IntelliCampus.Service.Specifications;

public sealed class ExamScheduleByExamIdSpec : BaseSpecifications<ExamSchedule>
{
    public ExamScheduleByExamIdSpec(int examId)
        : base(es => es.ExamId == examId)
    {
        AddOrderBy(es => es.Date);
    }
}
