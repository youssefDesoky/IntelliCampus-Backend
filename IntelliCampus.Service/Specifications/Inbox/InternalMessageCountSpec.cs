using IntelliCampus.Domain.Entities;
using IntelliCampus.Shared.Params;

namespace IntelliCampus.Service.Specifications;

internal sealed class InternalMessageCountSpec : BaseSpecifications<InternalMessage>
{
    private InternalMessageCountSpec(System.Linq.Expressions.Expression<Func<InternalMessage, bool>> criteria)
        : base(criteria)
    {
    }

    public static InternalMessageCountSpec InboxRoots(int userId, MessageQueryParams queryParams)
        => new(m =>
            m.RecipientId == userId &&
            !m.IsDeletedByRecipient &&
            m.ParentMessageId == null &&
            (string.IsNullOrEmpty(queryParams.Search) || m.Subject.Contains(queryParams.Search) || m.Body.Contains(queryParams.Search)) &&
            (!queryParams.DateFrom.HasValue || m.SentAt >= queryParams.DateFrom.Value) &&
            (!queryParams.DateTo.HasValue || m.SentAt <= queryParams.DateTo.Value));
}