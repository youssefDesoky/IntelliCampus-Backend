using IntelliCampus.Domain.Entities;

namespace IntelliCampus.Service.Specifications;

internal sealed class RemindersByStudentSpec : BaseSpecifications<Reminder>
{
    public RemindersByStudentSpec(int studentId)
        : base(r => r.StudentId == studentId)
    {
        AddOrderBy(r => r.Date);
    }
}
