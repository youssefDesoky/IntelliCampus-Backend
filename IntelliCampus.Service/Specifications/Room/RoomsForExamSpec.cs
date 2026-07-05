using IntelliCampus.Domain.Entities;

namespace IntelliCampus.Service.Specifications;

internal sealed class RoomsForExamSpec : BaseSpecifications<Room>
{
    public RoomsForExamSpec() : base(r => r.IsExamHall) { }
}
