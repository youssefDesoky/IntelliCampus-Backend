using IntelliCampus.Domain.Entities;

namespace IntelliCampus.Service.Specifications;

internal class UserByRecoveryEmailSpec : BaseSpecifications<User>
{
    public UserByRecoveryEmailSpec(string recoveryEmail)
        : base(u => u.RecoveryEmail == recoveryEmail)
    {
    }
}
