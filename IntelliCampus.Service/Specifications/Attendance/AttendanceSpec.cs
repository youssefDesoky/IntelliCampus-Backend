using IntelliCampus.Domain.Entities;

namespace IntelliCampus.Service.Specifications;

public class AttendanceSpec : BaseSpecifications<Attendance>
{
    public AttendanceSpec(int studentId)
        : base(a => a.StudentId == studentId)
    {
        AddOrderBy(a => a.Date);
    }
}
