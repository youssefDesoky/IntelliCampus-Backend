using IntelliCampus.Domain.Entities;
using IntelliCampus.Domain.Entities.Enums;

namespace IntelliCampus.Service.Specifications;

internal sealed class BroadcastSpec : BaseSpecifications<BroadcastAnnouncement>
{
    // Top 5 recent announcements (global, unfiltered)
    public BroadcastSpec()
    {
        AddOrderByDescending(b => b.CreatedAt);
        ApplyPagination(5, 1);
    }

    /// <summary>
    /// Broadcasts scoped to a faculty (all audiences) — used by admin dashboard.
    /// </summary>
    public BroadcastSpec(int? facultyId)
        : base(b => facultyId == null || b.FacultyId == null || b.FacultyId == facultyId)
    {
        AddOrderByDescending(b => b.CreatedAt);
        ApplyPagination(5, 1);
    }

    /// <summary>
    /// Broadcasts visible to an instructor — instructor's faculty + campus-wide,
    /// audience is All or Instructors.
    /// </summary>
    public BroadcastSpec(int? facultyId, bool forInstructors)
        : base(b => (facultyId == null || b.FacultyId == null || b.FacultyId == facultyId)
                 && (b.Audience == BroadcastAudience.All || b.Audience == BroadcastAudience.Instructors))
    {
        AddOrderByDescending(b => b.CreatedAt);
        ApplyPagination(5, 1);
    }

    /// <summary>
    /// Broadcasts visible to a student — student's faculty + campus-wide,
    /// audience is All or Students, and TargetStudentType matches (or is null).
    /// </summary>
    public BroadcastSpec(int? facultyId, StudentType studentType)
        : base(b => (facultyId == null || b.FacultyId == null || b.FacultyId == facultyId)
                 && (b.Audience == BroadcastAudience.All || b.Audience == BroadcastAudience.Students)
                 && (b.TargetStudentType == null || b.TargetStudentType == studentType))
    {
        AddOrderByDescending(b => b.CreatedAt);
        ApplyPagination(5, 1);
    }
}