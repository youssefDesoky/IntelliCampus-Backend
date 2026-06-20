using IntelliCampus.Domain.Entities;

namespace IntelliCampus.Service.Specifications;

internal sealed class RemindersByInstructorSpec : BaseSpecifications<Reminder>
{
    public RemindersByInstructorSpec(int instructorId)
        : base(r => r.InstructorId == instructorId)
    {
        AddOrderBy(r => r.Date);
    }
}
