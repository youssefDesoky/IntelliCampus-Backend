using System.Text.RegularExpressions;

namespace IntelliCampus.Service.Helpers;

public static partial class PhoneNormalizer
{
    public static string Normalize(string? phone)
    {
        if (string.IsNullOrWhiteSpace(phone))
            return string.Empty;

        var digits = DigitsOnly().Replace(phone, "");

        if (digits.Length == 12 && digits.StartsWith("20"))
            digits = "0" + digits[2..];

        return digits;
    }

    [GeneratedRegex(@"\D")]
    private static partial Regex DigitsOnly();
}
