namespace IntelliCampus.BLL.Utilities;

public static class SemesterHelper
{
    /// <summary>
    /// Determines the academic semester based on the given date.
    /// Fall: September - December
    /// Spring: January - April
    /// Summer: May - August
    /// </summary>
    public static string GetSemesterFromDate(DateTime date)
    {
        var month = date.Month;
        var year = date.Year;

        return month switch
        {
            >= 9 and <= 12 => $"Fall {year}",
            >= 1 and <= 4 => $"Spring {year}",
            >= 5 and <= 8 => $"Summer {year}",
            _ => $"Fall {year}" // Fallback, shouldn't happen
        };
    }

    /// <summary>
    /// Determines the academic semester based on the current date.
    /// </summary>
    public static string GetCurrentSemester()
    {
        return GetSemesterFromDate(DateTime.UtcNow);
    }
}
