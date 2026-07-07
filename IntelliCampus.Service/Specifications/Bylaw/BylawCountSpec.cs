using IntelliCampus.Domain.Entities;
using IntelliCampus.Domain.Entities.Enums;
using IntelliCampus.Shared.Params;

namespace IntelliCampus.Service.Specifications;

internal class BylawCountSpec : BaseSpecifications<Bylaw>
{
    public BylawCountSpec(BylawQueryParams queryParams)
        : base(b =>
            (string.IsNullOrEmpty(queryParams.Type)
                || b.Type == Enum.Parse<BylawType>(queryParams.Type, true)) &&
            (string.IsNullOrEmpty(queryParams.Search)
                || b.Name.Contains(queryParams.Search)
                || (b.NameAr != null && b.NameAr.Contains(queryParams.Search))) &&
            (!queryParams.FacultyId.HasValue || b.FacultyId == queryParams.FacultyId.Value))
    {
    }
}