using IntelliCampus.Domain.Entities;

namespace IntelliCampus.Service.Specifications;

internal sealed class ExamByDateSpec : BaseSpecifications<Exam>
{
    public ExamByDateSpec(DateTime date, int? excludeExamId)
        : base(e => e.Date.Date == date.Date && (excludeExamId == null || e.ExamId != excludeExamId.Value)) { }
}
