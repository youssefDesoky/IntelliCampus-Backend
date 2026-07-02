using System.Diagnostics;

namespace IntelliCampus.Domain.Helpers;

public static class EgyptTime
{
    private static readonly TimeZoneInfo EgyptZone = GetEgyptTimeZone();

    private static readonly object _lock = new();
    private static DateTime _cachedNow;
    private static long _lastFetchTicks;
    private static DateTime _cachedToday;
    private static DateOnly _cachedTodayDate;
    private static readonly double TtlFrequency = 0.5 * Stopwatch.Frequency;

    public static DateTime Now
    {
        get
        {
            var sw = Stopwatch.GetTimestamp();

            lock (_lock)
            {
                if (sw - _lastFetchTicks < (long)TtlFrequency)
                    return _cachedNow;

                var converted = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, EgyptZone);
                _cachedNow = converted;
                _lastFetchTicks = sw;
                return converted;
            }
        }
    }

    public static DateTime Today
    {
        get
        {
            var n = Now;

            lock (_lock)
            {
                if (DateOnly.FromDateTime(n) == _cachedTodayDate)
                    return _cachedToday;

                _cachedToday = n.Date;
                _cachedTodayDate = DateOnly.FromDateTime(n);
                return _cachedToday;
            }
        }
    }

    private static TimeZoneInfo GetEgyptTimeZone()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById("Egypt Standard Time");
        }
        catch
        {
            return TimeZoneInfo.FindSystemTimeZoneById("Africa/Cairo");
        }
    }
}
