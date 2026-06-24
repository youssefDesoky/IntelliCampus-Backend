using IntelliCampus.Domain.Entities;
using IntelliCampus.Domain.Entities.Enums;
using IntelliCampus.Shared.Params;

namespace IntelliCampus.Service.Specifications;

internal class BylawCountSpec : BaseSpecifications<Bylaw>
{
    public BylawCountSpec(BylawQueryParams queryParams)
        : base(b => string.IsNullOrEmpty(queryParams.Type)
            || b.Type == Enum.Parse<BylawType>(queryParams.Type, true))
    {
    }
}