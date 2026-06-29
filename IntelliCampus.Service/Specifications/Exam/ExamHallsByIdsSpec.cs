using IntelliCampus.Domain.Entities;

namespace IntelliCampus.Service.Specifications;

internal sealed class ExamHallsByIdsSpec : BaseSpecifications<ExamHall>
{
    public ExamHallsByIdsSpec(List<int> hallIds)
        : base(h => hallIds.Contains(h.ExamHallId)) { }
}
