using IntelliCampus.Domain.Entities;

namespace IntelliCampus.Service.Specifications;

internal sealed class RoomIdsSpec : BaseSpecifications<Room>
{
    public RoomIdsSpec(List<int> roomIds)
        : base(r => roomIds.Contains(r.RoomId)) { }
}
