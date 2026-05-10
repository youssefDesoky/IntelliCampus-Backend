using IntelliCampus.Domain.Entities;

namespace IntelliCampus.Service.Specifications
{
    internal class AdminByIdSpec : BaseSpecifications<Admin>
    {
        public AdminByIdSpec(int adminId)
        : base(a => a.UserId == adminId) { }
    }
}
