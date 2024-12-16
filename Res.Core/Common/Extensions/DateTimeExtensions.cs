namespace Res.Core.Common.Extensions
{
    public static class DateTimeExtensions
    {
        public static string ToAirlineFormat(this DateTime date)
        {
            return date.ToString("ddMMM HHmm").ToUpper();
        }
    }
}