using System;
using System.Globalization;

namespace Etsu.App
{
    public static class DateInput
    {
        public const string Format = "dd-MM-yyyy HH:mm:ss";

        public static bool TryParse(string text, out DateTime value)
        {
            value = DateTime.MinValue;
            if (text == null)
            {
                return false;
            }

            text = text.Trim();
            if (text.Length == 0)
            {
                return false;
            }

            return DateTime.TryParseExact(
                text,
                Format,
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out value);
        }
    }
}
