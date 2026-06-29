using IntelliCampus.Domain.Entities;
using IntelliCampus.Domain.Entities.Enums;

namespace IntelliCampus.Service.Specifications;

internal sealed class FriendRequestsByRecipientSpec : BaseSpecifications<FriendRequest>
{
    public FriendRequestsByRecipientSpec(int userId, FriendRequestStatus status)
        : base(fr => fr.RecipientId == userId && fr.Status == status)
    {
        AddOrderByDescending(fr => fr.CreatedAt);
    }
}
