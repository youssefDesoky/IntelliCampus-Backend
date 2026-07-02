namespace IntelliCampus.Domain.Helpers;

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
        return GetSemesterFromDate(EgyptTime.Now);
    }

    /// <summary>
    /// Returns the Arabic name of the current semester.
    /// </summary>
    public static string GetCurrentSemesterAr()
    {
        var month = EgyptTime.Now.Month;
        var year = EgyptTime.Now.Year;

        return month switch
        {
            >= 9 and <= 12 => $"الخريف {year}",
            >= 1 and <= 4 => $"الربيع {year}",
            >= 5 and <= 8 => $"الصيف {year}",
            _ => $"الخريف {year}"
        };
    }

    public static string? GetSemesterAr(string? semester)
    {
        if (string.IsNullOrWhiteSpace(semester))
            return null;

        var parts = semester.Split(' ');
        if (parts.Length < 2)
            return null;

        var year = parts[^1];
        var englishName = string.Join(' ', parts[..^1]);

        return englishName.ToLowerInvariant() switch
        {
            "fall" => $"الخريف {year}",
            "spring" => $"الربيع {year}",
            "summer" => $"الصيف {year}",
            _ => null
        };
    }
}
