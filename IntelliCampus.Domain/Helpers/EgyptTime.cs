namespace IntelliCampus.Domain.Helpers;

public static class EgyptTime
{
    private static readonly TimeZoneInfo EgyptZone =
        TimeZoneInfo.FindSystemTimeZoneById("Egypt Standard Time");

    public static DateTime Now => TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, EgyptZone);

    public static DateTime Today => Now.Date;
}
