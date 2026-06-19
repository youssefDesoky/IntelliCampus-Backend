using IntelliCampus.Domain.Entities;

namespace IntelliCampus.Service.Specifications;

internal sealed class ReminderByIdSpec : BaseSpecifications<Reminder>
{
    public ReminderByIdSpec(int reminderId)
        : base(r => r.ReminderId == reminderId)
    {
        AddInclude(r => r.Student!);
    }
}
