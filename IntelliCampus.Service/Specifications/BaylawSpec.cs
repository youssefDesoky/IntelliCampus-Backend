using IntelliCampus.Domain.Entities;

namespace IntelliCampus.Service.Specifications
{
    internal class BaylawSpec : BaseSpecifications<Baylaw>
    {
        public BaylawSpec()
        {
            AddInclude(b => b.UploadedBy);
            AddInclude(b => b.Students);
        }

        public BaylawSpec(int baylawId)
            : base(b => b.BaylawId == baylawId)
        {
            AddInclude(b => b.UploadedBy);
            AddInclude(b => b.Students);
        }
    }
}