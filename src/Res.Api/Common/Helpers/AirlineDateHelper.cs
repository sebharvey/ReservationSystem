namespace Res.Api.Common.Helpers
{
    public static class AirlineDateHelper
    {
        private static readonly string[] MonthAbbreviations =
        {
            "JAN", "FEB", "MAR", "APR", "MAY", "JUN", "JUL", "AUG", "SEP", "OCT", "NOV", "DEC"
        };

        /// <summary>
        /// Converts a DateTime to airline format (e.g., "20MAY")
        /// </summary>
        /// <param name="date">The DateTime to convert</param>
        /// <returns>String in airline date format</returns>
        public static string ToAirlineFormat(this DateTime date)
        {
            string day = date.Day.ToString("D2"); // Ensures 2 digits
            string month = MonthAbbreviations[date.Month - 1];
            return $"{day}{month}";
        }

        /// <summary>
        /// Converts an airline format date string (e.g., "20MAY") to DateTime
        /// </summary>
        /// <param name="airlineDate">The airline format date string</param>
        /// <returns>DateTime representation of the airline date</returns>
        /// <exception cref="ArgumentException">Thrown when the input string is invalid</exception>
        public static DateTime FromAirlineFormat(this string airlineDate)
        {
            if (string.IsNullOrWhiteSpace(airlineDate))
                throw new ArgumentException("Airline date cannot be null or empty", nameof(airlineDate));

            if (airlineDate.Length != 5)
                throw new ArgumentException("Airline date must be exactly 5 characters", nameof(airlineDate));

            if (!int.TryParse(airlineDate[..2], out int day))
                throw new ArgumentException("Invalid day format", nameof(airlineDate));

            string monthStr = airlineDate[2..].ToUpper();
            int month = Array.IndexOf(MonthAbbreviations, monthStr) + 1;

            if (month == 0)
                throw new ArgumentException("Invalid month format", nameof(airlineDate));

            int currentYear = DateTime.Now.Year;

            try
            {
                return new DateTime(currentYear, month, day);
            }
            catch (ArgumentOutOfRangeException)
            {
                throw new ArgumentException("Invalid date combination", nameof(airlineDate));
            }
        }

        /// <summary>
        /// Converts an airline format date and time string (e.g., "20MAY/2030") to DateTime
        /// Time format is in 24-hour format where 2030 represents 20:30 (8:30 PM)
        /// </summary>
        /// <param name="airlineDateAndTime">The airline format date and time string</param>
        /// <returns>DateTime representation of the airline date and time</returns>
        /// <exception cref="ArgumentException">Thrown when the input string is invalid</exception>
        public static DateTime FromAirlineDateAndTime(this string airlineDateAndTime)
        {
            if (string.IsNullOrWhiteSpace(airlineDateAndTime))
                throw new ArgumentException("Airline date and time cannot be null or empty", nameof(airlineDateAndTime));

            var parts = airlineDateAndTime.Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 2)
                throw new ArgumentException("Airline date and time must be in format '20MAY 2030'", nameof(airlineDateAndTime));

            var dateOnly = parts[0].FromAirlineFormat();

            if (!int.TryParse(parts[1], out int timeInt))
                throw new ArgumentException("Invalid time format", nameof(airlineDateAndTime));

            int hours = timeInt / 100;
            int minutes = timeInt % 100;

            if (hours < 0 || hours > 23 || minutes < 0 || minutes > 59)
                throw new ArgumentException("Invalid time values", nameof(airlineDateAndTime));

            return dateOnly.AddHours(hours).AddMinutes(minutes);
        }
    }
}