namespace IntelliCampus.Shared.Dtos.InstructorAnalytics;

public class WeeklyAttendanceItemDto
{
    public string Week { get; set; } = string.Empty;
    public int Present { get; set; }
    public int Absent { get; set; }
    public int Excused { get; set; }
}
