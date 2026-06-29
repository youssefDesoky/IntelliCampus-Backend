using IntelliCampus.Domain.Entities;

namespace IntelliCampus.Service.Specifications;

public class ChatMessageSpec : BaseSpecifications<ChatMessage>
{
    // GetChatHistoryAsync — two-user conversation, no includes
    public ChatMessageSpec(string userId1, string userId2)
        : base(m =>
            (m.SenderId == userId1 && m.RecipientId == userId2) ||
            (m.SenderId == userId2 && m.RecipientId == userId1))
    {
        AddOrderByDescending(m => m.Timestamp);
    }

    public ChatMessageSpec(string userId1, string userId2, int pageNumber, int pageSize)
        : this(userId1, userId2)
    {
        ApplyPagination(pageSize, pageNumber);
    }

    // GetGroupChatHistoryAsync — group messages, no includes
    public ChatMessageSpec(string groupName)
        : base(m => m.GroupName == groupName)
    {
        AddOrderByDescending(m => m.Timestamp);
    }

    public ChatMessageSpec(string groupName, int pageNumber, int pageSize)
        : this(groupName)
    {
        ApplyPagination(pageSize, pageNumber);
    }

    // FindPinnedMessageAsync — pinned messages in a conversation, no includes
    public ChatMessageSpec(string userId1, string userId2, bool pinned)
        : base(m => m.IsPinned &&
            ((m.SenderId == userId1 && m.RecipientId == userId2) ||
             (m.SenderId == userId2 && m.RecipientId == userId1))) { }

    // FindPinnedGroupMessageAsync — pinned messages in a group, no includes
    public ChatMessageSpec(string groupName, bool pinned)
        : base(m => m.GroupName == groupName && m.IsPinned) { }
}
