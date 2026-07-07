using IntelliCampus.Domain.Entities;
using IntelliCampus.Shared.Params;

namespace IntelliCampus.Service.Specifications;

internal class RoomCountSpec : BaseSpecifications<Room>
{
    public RoomCountSpec(RoomQueryParams queryParams)
        : base(r =>
            (string.IsNullOrEmpty(queryParams.Search) || r.RoomName.Contains(queryParams.Search)) &&
            (string.IsNullOrEmpty(queryParams.RoomType) || r.Type == queryParams.RoomType) &&
            (!queryParams.FacultyId.HasValue || r.FacultyId == queryParams.FacultyId))
    {
    }
}