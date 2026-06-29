using IntelliCampus.Domain.Entities;

namespace IntelliCampus.Service.Specifications;

internal sealed class ExamSeatAssignmentsByExamSpec : BaseSpecifications<ExamSeatAssignment>
{
    public ExamSeatAssignmentsByExamSpec(int examId)
        : base(a => a.ExamId == examId) { }
}
