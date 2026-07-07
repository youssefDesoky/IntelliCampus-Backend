namespace IntelliCampus.Domain.Entities.Enums;

/// <summary>
/// The audience a <see cref="BroadcastAnnouncement"/> is intended for
/// within the scope of the publishing admin's faculty.
/// </summary>
public enum BroadcastAudience
{
    /// <summary>
    /// Every user in the admin's faculty (students, instructors and admins).
    /// Used by SuperAdmin.
    /// </summary>
    All = 0,

    /// <summary>
    /// Only students whose <see cref="Student.StudentType"/> matches the broadcast's
    /// <see cref="BroadcastAnnouncement.TargetStudentType"/>. Used by typed admins
    /// (Admin_Bachelor, Admin_Masters, Admin_PhD, Admin_Diploma).
    /// </summary>
    Students = 1,

    /// <summary>
    /// Only instructors of the admin's faculty. Used by Admin_AcademicStaff.
    /// </summary>
    Instructors = 2,
}