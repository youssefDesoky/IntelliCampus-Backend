using IntelliCampus.Domain.Entities;

namespace IntelliCampus.Service.Specifications;

internal sealed class BroadcastSpec : BaseSpecifications<BroadcastAnnouncement>
{
    // Top 5 recent announcements
    public BroadcastSpec()
    {
        AddOrderByDescending(b => b.CreatedAt);
        ApplyPagination(5, 1);
    }
}
