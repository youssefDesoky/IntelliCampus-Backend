using IntelliCampus.Domain.Entities;

namespace IntelliCampus.Service.Specifications;

internal sealed class FriendshipsByUserIdSpec : BaseSpecifications<Friendship>
{
    public FriendshipsByUserIdSpec(int userId)
        : base(f => f.UserId1 == userId || f.UserId2 == userId) { }

    public FriendshipsByUserIdSpec(int userId, int pageSize, int pageIndex)
        : base(f => f.UserId1 == userId || f.UserId2 == userId)
    {
        ApplyPagination(pageSize, pageIndex);
    }
}
