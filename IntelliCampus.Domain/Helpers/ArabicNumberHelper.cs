using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IntelliCampus.Domain.Helpers
{
    public static class ArabicNumberHelper
    {
        private static readonly char[] ArabicDigits = ['٠', '١', '٢', '٣', '٤', '٥', '٦', '٧', '٨', '٩'];

        public static string? ToArabicDigits(this string? value)
        {
            if (string.IsNullOrEmpty(value)) return value;
            return string.Create(value.Length, value, (span, str) =>
            {
                for (int i = 0; i < str.Length; i++)
                {
                    var c = str[i];
                    span[i] = c is >= '0' and <= '9' ? ArabicDigits[c - '0'] : c;
                }
            });
        }
    }
}
