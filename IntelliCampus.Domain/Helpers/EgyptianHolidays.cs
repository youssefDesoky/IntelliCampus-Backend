namespace IntelliCampus.Domain.Helpers;

public static class EgyptianHolidays
{
    public static bool IsHoliday(DateTime date) =>
        date.DayOfWeek == DayOfWeek.Friday || FixedHolidays.Any(h =>
            h.Month == date.Month && h.Day == date.Day);

    public static bool IsHoliday(DateOnly date) =>
        date.DayOfWeek == DayOfWeek.Friday || FixedHolidays.Any(h =>
            h.Month == date.Month && h.Day == date.Day);

    public static readonly List<(int Month, int Day)> FixedHolidays =
    [
        (1, 1),    // New Year
        (1, 7),    // Coptic Christmas
        (4, 25),   // Sinai Liberation Day
        (5, 1),    // Labor Day
        (7, 23),   // Revolution Day
        (10, 6),   // Armed Forces Day
    ];

    public static List<DateTime> GetHolidayDates(DateOnly from, DateOnly to)
    {
        var holidays = new List<DateTime>();
        for (var d = from; d <= to; d = d.AddDays(1))
        {
            if (IsHoliday(d))
                holidays.Add(d.ToDateTime(TimeOnly.MinValue));
        }
        return holidays;
    }

    public static List<DateOnly> GetWorkingDays(DateOnly from, DateOnly to)
    {
        var working = new List<DateOnly>();
        for (var d = from; d <= to; d = d.AddDays(1))
        {
            if (!IsHoliday(d))
                working.Add(d);
        }
        return working;
    }
}
