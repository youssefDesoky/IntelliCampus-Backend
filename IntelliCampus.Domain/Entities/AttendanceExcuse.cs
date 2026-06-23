using IntelliCampus.Domain.Entities.Enums;

namespace IntelliCampus.Domain.Entities;

public class AttendanceExcuse
{
    public int          ExcuseId             { get; set; }
    public int          StudentId            { get; set; }
    public Student?     Student              { get; set; }
    public int          SessionId            { get; set; }
    public Session?     Session              { get; set; }
    public string?      Reason               { get; set; }
    public ExcuseStatus Status               { get; set; } = ExcuseStatus.Pending;
    public DateTime     CreatedAt            { get; set; } = EgyptTime.Now;

    // ── Supporting document (optional) ──────────────────────────────────────
    public string?      DocumentPath         { get; set; }  // server-side storage path
    public string?      DocumentOriginalName { get; set; }  // original filename shown in UI
    public string?      DocumentContentType  { get; set; }  // MIME type for serving
}
