using IntelliCampus.Domain.Entities;
using IntelliCampus.Shared.Params;

namespace IntelliCampus.Service.Specifications;

internal sealed class RoomSpec : BaseSpecifications<Room>
{
    public RoomSpec()
    {
    }

    public RoomSpec(int pageSize, int pageIndex)
    {
        ApplyPagination(pageSize, pageIndex);
    }

    public RoomSpec(RoomQueryParams queryParams)
        : base(r =>
            (string.IsNullOrEmpty(queryParams.Search) || r.RoomName.Contains(queryParams.Search)) &&
            (string.IsNullOrEmpty(queryParams.RoomType) || r.Type == queryParams.RoomType))
    {
        ApplyPagination(queryParams.PageSize, queryParams.PageIndex);
    }
}
