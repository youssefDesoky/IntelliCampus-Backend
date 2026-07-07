using IntelliCampus.Domain.Entities;
using IntelliCampus.Shared.Params;

namespace IntelliCampus.Service.Specifications;

internal sealed class RoomSpec : BaseSpecifications<Room>
{
    public RoomSpec()
    {
        AddInclude(r => r.Faculty!);
    }

    public RoomSpec(int pageSize, int pageIndex)
    {
        AddInclude(r => r.Faculty!);
        ApplyPagination(pageSize, pageIndex);
    }

    public RoomSpec(RoomQueryParams queryParams)
        : base(r =>
            (string.IsNullOrEmpty(queryParams.Search) || r.RoomName.Contains(queryParams.Search)) &&
            (string.IsNullOrEmpty(queryParams.RoomType) || r.Type == queryParams.RoomType) &&
            (!queryParams.FacultyId.HasValue || r.FacultyId == queryParams.FacultyId))
    {
        AddInclude(r => r.Faculty!);
        ApplyPagination(queryParams.PageSize, queryParams.PageIndex);
    }
}
