using System.Linq.Expressions;
using IntelliCampus.Domain.Entities;
using IntelliCampus.Shared.Params;
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

    /// <summary>
    /// Root messages received by the user.
    /// </summary>
    public static InternalMessageSpec InboxRoots(int userId)
    {
        var spec = new InternalMessageSpec(m =>
            m.RecipientId == userId &&
            !m.IsDeletedByRecipient &&
            m.ParentMessageId == null);
        spec.AddOrderByDescending(m => m.SentAt);
        return spec;
    }

    public static InternalMessageSpec InboxRoots(int userId, MessageQueryParams queryParams)
    {
        var spec = new InternalMessageSpec(m =>
            m.RecipientId == userId &&
            !m.IsDeletedByRecipient &&
            m.ParentMessageId == null &&
            (string.IsNullOrEmpty(queryParams.Search) || m.Subject.Contains(queryParams.Search) || m.Body.Contains(queryParams.Search)) &&
            (!queryParams.DateFrom.HasValue || m.SentAt >= queryParams.DateFrom.Value) &&
            (!queryParams.DateTo.HasValue || m.SentAt <= queryParams.DateTo.Value));
        spec.AddOrderByDescending(m => m.SentAt);
        spec.ApplyPagination(queryParams.PageSize, queryParams.PageIndex);
        return spec;
    }

    /// <summary>
    /// Root messages sent by the user.
    /// </summary>
    public static InternalMessageSpec SentRoots(int userId)
    {
        var spec = new InternalMessageSpec(m =>
            m.SenderId == userId &&
            !m.IsDeletedBySender &&
            m.ParentMessageId == null);
        spec.AddOrderByDescending(m => m.SentAt);
        return spec;
    }

    public static InternalMessageSpec SentRoots(int userId, MessageQueryParams queryParams)
    {
        var spec = new InternalMessageSpec(m =>
            m.SenderId == userId &&
            !m.IsDeletedBySender &&
            m.ParentMessageId == null &&
            (string.IsNullOrEmpty(queryParams.Search) || m.Subject.Contains(queryParams.Search) || m.Body.Contains(queryParams.Search)) &&
            (!queryParams.DateFrom.HasValue || m.SentAt >= queryParams.DateFrom.Value) &&
            (!queryParams.DateTo.HasValue || m.SentAt <= queryParams.DateTo.Value));
        spec.AddOrderByDescending(m => m.SentAt);
        return spec;
    }

    /// <summary>
    /// All replies to the given root messages that the user can see.
    /// </summary>
    public static InternalMessageSpec RepliesToRoots(IEnumerable<int> rootIds, int userId)
    {
        var ids = rootIds.ToList();
        var spec = new InternalMessageSpec(m =>
            ids.Contains(m.ParentMessageId ?? 0) &&
            ((m.SenderId == userId && !m.IsDeletedBySender) ||
             (m.RecipientId == userId && !m.IsDeletedByRecipient)));
        spec.AddOrderBy(m => m.SentAt);
        return spec;
    }
}
