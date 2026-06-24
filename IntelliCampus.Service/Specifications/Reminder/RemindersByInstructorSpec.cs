using IntelliCampus.Domain.Entities;
using IntelliCampus.Shared.Params;

namespace IntelliCampus.Service.Specifications;

internal sealed class RemindersByInstructorSpec : BaseSpecifications<Reminder>
{
    public RemindersByInstructorSpec(int instructorId)
        : base(r => r.InstructorId == instructorId)
    {
        AddOrderBy(r => r.Date);
    }

    public RemindersByInstructorSpec(int instructorId, ReminderQueryParams queryParams)
        : base(r => r.InstructorId == instructorId
            && (!queryParams.SelectedDay.HasValue
                || (r.Date >= queryParams.SelectedDay.Value.ToDateTime(TimeOnly.MinValue)
                    && r.Date < queryParams.SelectedDay.Value.AddDays(8).ToDateTime(TimeOnly.MinValue))))
    {
        AddOrderBy(r => r.Date);
    }
}
