using IntelliCampus.Domain.Entities;
using IntelliCampus.Domain.Entities.Enums;
using IntelliCampus.Shared.Params;

namespace IntelliCampus.Service.Specifications
{
    internal class BylawSpec : BaseSpecifications<Bylaw>
    {
        public BylawSpec()
        {
            AddInclude(b => b.UploadedBy!);
            AddInclude(b => b.Students!);
        }

        public BylawSpec(int bylawId)
            : base(b => b.BylawId == bylawId)
        {
            AddInclude(b => b.UploadedBy!);
            AddInclude(b => b.Students!);
        }

        public BylawSpec(BylawQueryParams queryParams)
            : base(b => string.IsNullOrEmpty(queryParams.Type)
                || b.Type == Enum.Parse<BylawType>(queryParams.Type, true))
        {
            AddInclude(b => b.UploadedBy!);
            AddInclude(b => b.Students!);
        }
    }
}
