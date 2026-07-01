using IntelliCampus.Domain.Helpers;

namespace IntelliCampus.Domain.Entities;

public class BroadcastAnnouncement
{
    public int Id { get; set; }
    public int SenderId { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = EgyptTime.Now;
    public DateTime UpdatedAt { get; set; } = EgyptTime.Now;

    public Admin Sender { get; set; } = null!;
}
