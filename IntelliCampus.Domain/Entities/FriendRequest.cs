using IntelliCampus.Domain.Entities.Enums;

namespace IntelliCampus.Domain.Entities;

public class FriendRequest
{
    public int FriendRequestId { get; set; }
    public int SenderId { get; set; }
    public int RecipientId { get; set; }
    public FriendRequestStatus Status { get; set; } = FriendRequestStatus.Pending;
    public DateTime CreatedAt { get; set; } = EgyptTime.Now;

    public User Sender { get; set; } = null!;
    public User Recipient { get; set; } = null!;
}
