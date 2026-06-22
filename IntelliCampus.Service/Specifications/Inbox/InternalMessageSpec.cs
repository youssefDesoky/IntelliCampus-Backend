using System.Linq.Expressions;
using IntelliCampus.Domain.Entities;
using IntelliCampus.Service.Specifications;

namespace IntelliCampus.Service.Specifications;

public sealed class InternalMessageSpec : BaseSpecifications<InternalMessage>
{
    private InternalMessageSpec(Expression<Func<InternalMessage, bool>> criteria)
        : base(criteria)
    {
    }

    public static InternalMessageSpec ById(int messageId)
        => new(m => m.MessageId == messageId);

    public static InternalMessageSpec Inbox(int userId)
        => Inbox(userId, false);

    public static InternalMessageSpec Inbox(int userId, bool includeRead)
    {
        var spec = new InternalMessageSpec(m =>
            m.RecipientId == userId &&
            !m.IsDeletedByRecipient &&
            (includeRead || !m.IsRead));
        spec.AddOrderByDescending(m => m.SentAt);
        return spec;
    }

    public static InternalMessageSpec Sent(int userId)
    {
        var spec = new InternalMessageSpec(m =>
            m.SenderId == userId &&
            !m.IsDeletedBySender);
        spec.AddOrderByDescending(m => m.SentAt);
        return spec;
    }
}
