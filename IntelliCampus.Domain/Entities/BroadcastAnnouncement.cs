using IntelliCampus.Domain.Entities.Enums;
using IntelliCampus.Domain.Helpers;

namespace IntelliCampus.Domain.Entities;

public class BroadcastAnnouncement
{
    public int Id { get; set; }
    public int SenderId { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = EgyptTime.Now;
    public DateTime UpdatedAt { get; set; } = EgyptTime.Now;

    /// <summary>
    /// Faculty the broadcast is scoped to. <c>null</c> is reserved for campus-wide
    /// broadcasts (SuperAdmin with no faculty assigned).
    /// </summary>
    public int? FacultyId { get; set; }

    /// <summary>
    /// Audience within <see cref="FacultyId"/> that should see the broadcast.
    /// </summary>
    public BroadcastAudience Audience { get; set; } = BroadcastAudience.All;

    /// <summary>
    /// When <see cref="Audience"/> is <see cref="BroadcastAudience.Students"/>,
    /// specifies the student type (Bachelor/Masters/PhD/Diploma) the broadcast targets.
    /// <c>null</c> matches all student types.
    /// </summary>
    public StudentType? TargetStudentType { get; set; }

    public Admin Sender { get; set; } = null!;
    public Faculty? Faculty { get; set; }
}
