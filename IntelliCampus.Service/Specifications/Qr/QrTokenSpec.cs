using IntelliCampus.Domain.Entities;

namespace IntelliCampus.Service.Specifications;

public class QrTokenSpec : BaseSpecifications<QrToken>
{
    public QrTokenSpec(string token)
        : base(q => q.Token == token
                 && q.ExpiresAt > DateTime.UtcNow)
    {
        AddInclude(q => q.Student!);
    }

}
