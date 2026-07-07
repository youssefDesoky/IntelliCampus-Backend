using IntelliCampus.Domain.Entities;

namespace IntelliCampus.Service.Specifications;

public class AttendanceSpec : BaseSpecifications<Attendance>
{
    public AttendanceSpec(int studentId)
        : base(a => a.StudentId == studentId)
    {
        AddOrderBy(a => a.Date);
    }

    // Admin dashboard — date range to limit data
    public AttendanceSpec(DateTime from, DateTime to)
        : base(a => a.Date >= from && a.Date <= to)
    {
        AddOrderBy(a => a.Date);
        AddInclude("Student.User");
    }

    // Session attendance — by session and student IDs
    public AttendanceSpec(int sessionId, HashSet<int> studentIds)
        : base(a => a.SessionId == sessionId && studentIds.Contains(a.StudentId)) { }

    // Session attendance — all records for a session (use bool to disambiguate from studentId)
    public AttendanceSpec(int sessionId, bool bySession)
        : base(a => a.SessionId == sessionId) { }
}
