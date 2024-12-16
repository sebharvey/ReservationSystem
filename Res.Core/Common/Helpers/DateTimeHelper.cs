using System.Globalization;

namespace Res.Core.Common.Helpers
{
    public static class DateTimeHelper
    {
        public static DateTime CombineDateAndTime(string dateStr, string timeStr)
        {
            return DateTime.ParseExact($"{dateStr} {timeStr}", "ddMMM HHmm", CultureInfo.InvariantCulture);
        }

        public static TimeSpan ParseAirlineTime(string time)
        {
            return TimeSpan.ParseExact(time, "HHmm", CultureInfo.InvariantCulture);
        }

        public static DateTime ParseAirlineLongDate(string dateStr)
        {
            return DateTime.ParseExact(dateStr, "ddMMMyy", CultureInfo.InvariantCulture);
        }

        public static DateTime ParseAirlineLongDateAndYear(string dateStr)
        {
            return DateTime.ParseExact(dateStr, "ddMMMyyyy", CultureInfo.InvariantCulture);
        }
    }
}