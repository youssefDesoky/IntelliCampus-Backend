using IntelliCampus.Domain.Entities;

namespace IntelliCampus.Service.Specifications;

internal class UserByNationalIdSpec : BaseSpecifications<User>
{
    public UserByNationalIdSpec(string nationalId)
        : base(u => u.NationalId == nationalId)
    {
    }
}
