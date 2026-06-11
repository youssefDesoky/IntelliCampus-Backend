using IntelliCampus.Domain.Entities;

namespace IntelliCampus.Service.Specifications
{
    internal class BylawSpec : BaseSpecifications<Bylaw>
    {
        public BylawSpec()
        {
            AddInclude(b => b.UploadedBy);
            AddInclude(b => b.Students);
        }

        public BylawSpec(int bylawId)
            : base(b => b.BylawId == bylawId)
        {
            AddInclude(b => b.UploadedBy);
            AddInclude(b => b.Students);
        }
    }
}
