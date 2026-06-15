using IntelliCampus.Domain.Entities;

namespace IntelliCampus.Service.Specifications;

public class AttendanceExcuseSpec : BaseSpecifications<AttendanceExcuse>
{
    // GetByStudent
    public AttendanceExcuseSpec(int studentId) 
        : base(e => e.StudentId == studentId)
    {
        AddInclude(e => e.Session!);
        AddOrderByDescending(e => e.CreatedAt);
    }

    // GetBySession (for instructors)
    public AttendanceExcuseSpec(int sessionId, bool bySession)
        : base(e => e.SessionId == sessionId)
    {
        AddInclude(e => e.Student!);
        AddOrderByDescending(e => e.CreatedAt);
    }
}
