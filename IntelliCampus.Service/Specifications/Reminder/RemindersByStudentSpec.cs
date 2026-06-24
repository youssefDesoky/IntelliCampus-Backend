using IntelliCampus.Domain.Entities;
using IntelliCampus.Shared.Params;

namespace IntelliCampus.Service.Specifications;

internal sealed class RemindersByStudentSpec : BaseSpecifications<Reminder>
{
    public RemindersByStudentSpec(int studentId)
        : base(r => r.StudentId == studentId)
    {
        AddOrderBy(r => r.Date);
    }

    public RemindersByStudentSpec(int studentId, ReminderQueryParams queryParams)
        : base(r => r.StudentId == studentId
            && (!queryParams.SelectedDay.HasValue
                || (r.Date >= queryParams.SelectedDay.Value.ToDateTime(TimeOnly.MinValue)
                    && r.Date < queryParams.SelectedDay.Value.AddDays(8).ToDateTime(TimeOnly.MinValue))))
    {
        AddOrderBy(r => r.Date);
    }
}
