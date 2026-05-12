namespace IntelliCampus.Shared.Dtos.Reminder;
public class RemindersGroupedDto
{
    public List<ReminderDto> SelectedDay { get; set; } = [];
    public List<ReminderDto> NextDay { get; set; } = [];
    public List<ReminderDto> Week { get; set; } = [];
}
